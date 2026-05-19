import { NavLink } from 'react-router-dom';

interface AppNavLinkProps {
  to: string;
  children: React.ReactNode;
}

export default function AppNavLink({ to, children }: AppNavLinkProps) {
  return (
    <NavLink to={to} className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
      {children}
    </NavLink>
  );
}
