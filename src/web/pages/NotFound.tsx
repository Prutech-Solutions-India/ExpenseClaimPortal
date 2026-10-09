import { Link } from 'react-router-dom';

export default function NotFound() {
  return (
    <section>
      <h1>Page not found</h1>
      <p>
        Nothing lives at this address. <Link to="/">Back to the start</Link>.
      </p>
    </section>
  );
}
