interface ErrorAlertProps {
  title?: string;
  messages: string[];
}

export const ErrorAlert = ({ title = "Request failed", messages }: ErrorAlertProps) => {
  if (messages.length === 0) {
    return null;
  }

  return (
    <div className="alert alert-danger" role="alert">
      <strong>{title}</strong>
      <ul className="alert-list">
        {messages.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </div>
  );
};
