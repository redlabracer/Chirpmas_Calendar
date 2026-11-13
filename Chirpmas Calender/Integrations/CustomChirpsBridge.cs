using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ChirpmasCalendar.Diagnostics;
using Unity.Entities;

namespace ChirpmasCalendar.Integrations
{
    
    // Soft bridge to the Custom Chirps mod (ruzbeh0, Paradox Mods 121812), resolved via reflection so the calendar
    // still works when it is missing. API: CustomChirps.Systems.CustomChirpApiSystem (thread-safe static methods).
    
    public static class CustomChirpsBridge
    {
        public const int kPdxModId = 121812;

        private const string kApiTypeName = "CustomChirps.Systems.CustomChirpApiSystem";
        private const string kDepartmentTypeName = "CustomChirps.Systems.DepartmentAccount";

        private const string kDepartment = "ParkAndRec";

        private static bool s_Resolved;

        // Compiled once in Resolve(): (text, sender) and (text, imageUrl, sender), with the department and Entity.Null baked in.
        private static Action<string, string> s_PostChirp;
        private static Action<string, string, string> s_PostLargeWithImage;

        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return s_PostChirp != null;
            }
        }

        public static void PostChirp(string text, string senderName)
        {
            Action<string, string> post = s_PostChirp ?? throw new InvalidOperationException("Custom Chirps is not available");
            post(text ?? string.Empty, senderName);
        }

        public static void PostChirpWithImage(string text, string imageUrl, string senderName)
        {
            if (s_PostLargeWithImage == null || string.IsNullOrEmpty(imageUrl))
            {
                PostChirp(text, senderName);
                return;
            }

            s_PostLargeWithImage(text ?? string.Empty, imageUrl, senderName);
        }

        // Lets the next <see cref="IsAvailable"/> look for Custom Chirps again if it wasn't found before (called once per loaded city)
        internal static void RetryIfMissing()
        {
            if (s_PostChirp == null)
                s_Resolved = false;
        }

        private static void Resolve()
        {
            if (s_Resolved)
                return;
            s_Resolved = true;

            using var profile = ModProfiler.CustomChirpsResolve.Auto();

            Type apiType = FindType(kApiTypeName);
            Type departmentType = FindType(kDepartmentTypeName);
            if (apiType == null || departmentType == null)
                return;

            try
            {
                object department = Enum.Parse(departmentType, kDepartment);
                MethodInfo postChirp = apiType.GetMethod("PostChirp", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(string), departmentType, typeof(Entity), typeof(string) }, null);
                MethodInfo postLargeWithImage = apiType.GetMethod("PostLargeChirpWithPortraitImage", BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(string), departmentType, typeof(Entity), typeof(string), typeof(string) }, null);

                s_PostChirp = Bind<Action<string, string>>(postChirp, department, departmentType, 1);
                s_PostLargeWithImage = Bind<Action<string, string, string>>(postLargeWithImage, department, departmentType, 2);
            }
            catch (Exception e)
            {
                Mod.Log.Warn(e, "Custom Chirps was found but its API could not be resolved");
                s_PostChirp = null;
                s_PostLargeWithImage = null;
            }

            Mod.Log.Info($"Custom Chirps API: chirps={s_PostChirp != null}, images={s_PostLargeWithImage != null}");
        }

        // Builds (text, [imageUrl,] sender) => method(text, department, Entity.Null, [imageUrl,] sender).
        // stringsAfterEntity = number of string parameters following the Entity (sender, or imageUrl + sender).
        private static TDelegate Bind<TDelegate>(MethodInfo method, object department, Type departmentType, int stringsAfterEntity)
            where TDelegate : Delegate
        {
            if (method == null)
                return null;

            var parameters = new ParameterExpression[1 + stringsAfterEntity];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Expression.Parameter(typeof(string));

            var arguments = new Expression[parameters.Length + 2];
            arguments[0] = parameters[0];
            arguments[1] = Expression.Constant(department, departmentType);
            arguments[2] = Expression.Constant(Entity.Null, typeof(Entity));
            for (int i = 1; i < parameters.Length; i++)
                arguments[i + 2] = parameters[i];

            try
            {
                return Expression.Lambda<TDelegate>(Expression.Call(method, arguments), parameters).Compile();
            }
            catch (Exception e)
            {
                // Should not happen on the game's Mono runtime; reflection still works, it just allocates per call.
                Mod.Log.Warn(e, $"Could not compile a delegate for Custom Chirps {method.Name}; using reflection");
                return (TDelegate)(Delegate)ReflectionFallback(method, department, stringsAfterEntity);
            }
        }

        private static Delegate ReflectionFallback(MethodInfo method, object department, int stringsAfterEntity)
        {
            if (stringsAfterEntity == 1)
                return new Action<string, string>((text, sender) => Invoke(method, text, department, Entity.Null, sender));

            return new Action<string, string, string>((text, imageUrl, sender) => Invoke(method, text, department, Entity.Null, imageUrl, sender));
        }

        private static void Invoke(MethodInfo method, params object[] args)
        {
            try
            {
                method.Invoke(null, args);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);
                    if (type != null)
                        return type;
                }
                catch (Exception e)
                {
                    // Some (dynamic or partially loaded) assemblies throw on type lookup; they can't contain Custom Chirps anyway.
                    Mod.Log.Debug($"Skipped assembly {assembly.GetName().Name} while looking for {fullName}: {e.Message}");
                }
            }

            return null;
        }
    }
}
