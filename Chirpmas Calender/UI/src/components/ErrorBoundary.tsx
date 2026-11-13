import { Component, ErrorInfo, ReactNode } from "react";

interface ErrorBoundaryProps {
    name: string;
    children?: ReactNode;
}

interface ErrorBoundaryState {
    failed: boolean;
}

/**
 * Keeps a rendering error inside this mod: without it, an exception in our components unmounts the whole game UI.
 * The failed subtree renders nothing; the error is logged to the UI console / Player.log.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
    state: ErrorBoundaryState = { failed: false };

    static getDerivedStateFromError(): ErrorBoundaryState {
        return { failed: true };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error(`[ChirpmasCalendar] ${this.props.name} crashed and was disabled`, error, info.componentStack);
    }

    render() {
        return this.state.failed ? null : this.props.children;
    }
}

/** Wraps a component so it renders inside an {@link ErrorBoundary}. */
export function withErrorBoundary(name: string, Inner: () => JSX.Element | null) {
    const Wrapped = () => (
        <ErrorBoundary name={name}>
            <Inner />
        </ErrorBoundary>
    );
    return Wrapped;
}
