import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import App from '@/App';

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

function respondWith(status: number, body: unknown) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response);
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('App routing', () => {
  it('shows the home page at the root', () => {
    renderAt('/');
    expect(screen.getByRole('heading', { name: 'Pilot service' })).toBeInTheDocument();
  });

  it('shows a not-found page for an unknown address', () => {
    renderAt('/nothing-here');
    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument();
  });
});

describe('the greeting form', () => {
  it('sends the name to the API and renders what comes back', async () => {
    const fetchMock = respondWith(201, { greeting: 'Hello, Ada.' });
    vi.stubGlobal('fetch', fetchMock);

    renderAt('/');
    await userEvent.type(screen.getByLabelText('Your name'), 'Ada');
    await userEvent.click(screen.getByRole('button', { name: 'Send' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Hello, Ada.');
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/greetings',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ name: 'Ada' }) }),
    );
  });

  it('puts the API validation message on the page rather than failing silently', async () => {
    vi.stubGlobal(
      'fetch',
      respondWith(400, { title: 'Bad Request', errors: { Name: ['name must not be empty'] } }),
    );

    renderAt('/');
    await userEvent.click(screen.getByRole('button', { name: 'Send' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('name must not be empty');
  });
});
