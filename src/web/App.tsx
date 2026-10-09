/**
 * The application shell.
 *
 * One header carrying the product title and the acting-identity switcher, one
 * `<main>` carrying the routed page. The router table lives here and only here
 * - a second place that decides what a URL renders is a second answer to the
 * same question.
 *
 * There is no Razor and no Blazor behind this: every non-`/api` path falls back
 * to this shell on the server and React decides what to show, including its own
 * not-found page.
 */

import type { ReactElement } from 'react';
import { Link, Route, Routes } from 'react-router-dom';

import { UserSwitcher } from '@/components/UserSwitcher';
import Home from '@/pages/Home';
import NotFound from '@/pages/NotFound';

/** Renders the header, the switcher and the route table. */
export default function App(): ReactElement {
  return (
    <div className="app-shell">
      <header className="app-header">
        <h1 className="app-header__title">
          <Link to="/">Expense claim portal</Link>
        </h1>
        <UserSwitcher />
      </header>
      <main className="app-main">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </div>
  );
}
