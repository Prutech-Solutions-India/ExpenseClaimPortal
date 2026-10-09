/**
 * The entry point.
 *
 * Exactly one router and exactly one identity provider are mounted, both here,
 * wrapping the whole tree. Two of either would mean two answers to "which route
 * is this" or "who am I acting as", and only one of them would be the one the
 * server sees.
 */

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import { IdentityProvider } from '@/lib/identity';

import App from './App';
import './styles.css';

const container = document.getElementById('root');
if (!container) {
  throw new Error('No #root element to mount into.');
}

createRoot(container).render(
  <StrictMode>
    <BrowserRouter>
      <IdentityProvider>
        <App />
      </IdentityProvider>
    </BrowserRouter>
  </StrictMode>,
);
