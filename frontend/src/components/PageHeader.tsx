import { ReactNode } from "react";

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

export const PageHeader = ({ title, description, actions }: PageHeaderProps) => (
  <div className="page-header">
    <div>
      <h1>{title}</h1>
      {description ? <p>{description}</p> : null}
    </div>
    {actions ? <div className="page-header-actions">{actions}</div> : null}
  </div>
);
