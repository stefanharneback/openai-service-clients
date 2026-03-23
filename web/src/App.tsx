import { useState } from "react";

import { getHealth, type HealthResponse } from "./api/client";

const defaultBaseUrl = "http://localhost:3000";

function App() {
  const [baseUrl, setBaseUrl] = useState(defaultBaseUrl);
  const [result, setResult] = useState<HealthResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const runHealthCheck = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await getHealth(baseUrl);
      setResult(data);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
      setResult(null);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="page">
      <section className="card">
        <h1>OpenAI Service Clients</h1>
        <p>Starter web client for full-chain gateway testing.</p>

        <label htmlFor="base-url">Gateway base URL</label>
        <input
          id="base-url"
          value={baseUrl}
          onChange={(event) => setBaseUrl(event.target.value)}
        />

        <button onClick={runHealthCheck} disabled={loading}>
          {loading ? "Checking..." : "Check /health"}
        </button>

        {error ? <pre className="error">{error}</pre> : null}
        {result ? <pre>{JSON.stringify(result, null, 2)}</pre> : null}
      </section>
    </main>
  );
}

export default App;
