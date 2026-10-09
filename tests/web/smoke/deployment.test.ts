// @vitest-environment node
import { describe, expect, it } from 'vitest';

const baseUrl = process.env.BASE_URL?.replace(/\/$/, '') ?? '';

// Skipped rather than passed when there is no deployment to talk to. A smoke
// suite that goes green against nothing is the failure this whole pipeline
// exists to prevent.
describe.skipIf(baseUrl === '')('the deployed application', () => {
  it('serves the application shell', async () => {
    const response = await fetch(`${baseUrl}/`);

    expect(response.status).toBe(200);
    expect(await response.text()).toContain('<div id="root">');
  });

  // The whole point of this stack is that one deployment carries both layers.
  // Checking only the shell would pass against a static bundle with no service
  // behind it, which is exactly the half-deployment worth catching here.
  it('serves the API the shell calls', async () => {
    const response = await fetch(`${baseUrl}/api/health`);

    expect(response.status).toBe(200);
    expect(await response.json()).toMatchObject({ status: 'ok' });
  });
});
