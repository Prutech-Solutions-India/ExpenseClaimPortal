import { useState } from 'react';

import { ApiError, createGreeting } from '../lib/api';
import { buildInfo } from '../lib/build-info';

/**
 * The seed the delivery pipeline builds on, and a worked example of the one
 * thing this stack exists to prove: a screen that calls its own API.
 */
export default function Home() {
  const info = buildInfo();
  const [name, setName] = useState('');
  const [greeting, setGreeting] = useState('');
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setPending(true);
    setGreeting('');
    setError('');
    try {
      const result = await createGreeting(name);
      setGreeting(result.greeting);
    } catch (cause) {
      // A failed call has to be visible on the page. Logging it to the console
      // and rendering nothing is indistinguishable from the request never
      // having been made.
      setError(cause instanceof ApiError ? cause.message : 'The service could not be reached.');
    } finally {
      setPending(false);
    }
  }

  return (
    <section>
      <h1>Pilot service</h1>
      <p>
        This page is the seed the delivery pipeline builds on. Replace it with the
        first story&apos;s work.
      </p>

      <form onSubmit={onSubmit}>
        <label htmlFor="name">Your name</label>
        <input
          id="name"
          name="name"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
        <button type="submit" disabled={pending}>
          {pending ? 'Sending...' : 'Send'}
        </button>
      </form>

      {greeting !== '' && <p role="status">{greeting}</p>}
      {error !== '' && <p role="alert">{error}</p>}

      <dl>
        <dt>Version</dt>
        <dd>{info.version}</dd>
        <dt>Commit</dt>
        <dd>{info.commit}</dd>
      </dl>
    </section>
  );
}
