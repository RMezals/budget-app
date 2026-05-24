import { NavLink } from 'react-router-dom';

interface AppNavLinkProps {
  to: string;
  icon?: React.ReactNode;
  children: React.ReactNode;
}

export default function AppNavLink({ to, icon, children }: AppNavLinkProps) {
  const isRoot = to === '/';
  return (
    <NavLink
      to={to}
      end={isRoot}
      className={({ isActive }) => `app-nav-link${isActive ? ' active' : ''}`}
    >
      {icon}
      <span className="nav-label">{children}</span>
    </NavLink>
  );
}
