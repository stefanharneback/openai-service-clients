import { useState } from "react";

import { getHealth, sendLlm, type HealthResponse } from "./api/client";

const defaultBaseUrl = "http://localhost:3000";

function App() {
  const [baseUrl, setBaseUrl] = useState(defaultBaseUrl);
  const [result, setResult] = useState<HealthResponse | null>(null);
  const [llmResult, setLlmResult] = useState<unknown>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [llmLoading, setLlmLoading] = useState(false);
  const [apiKey, setApiKey] = useState("");
  const [model, setModel] = useState("gpt-5.4-mini");
  const [input, setInput] = useState("Summarize the value of API gateways in three bullets.");

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

  const runLlm = async () => {
    setLlmLoading(true);
    setError(null);

    try {
      const payload = await sendLlm(baseUrl, apiKey, {
        model,
        input,
        stream: false,
      });
      setLlmResult(payload);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
      setLlmResult(null);
    } finally {
      setLlmLoading(false);
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

        <h2>LLM (non-stream)</h2>

        <label htmlFor="api-key">Client API key</label>
        <input
          id="api-key"
          type="password"
          value={apiKey}
          onChange={(event) => setApiKey(event.target.value)}
        />

        <label htmlFor="model">Model</label>
        <input
          id="model"
          value={model}
          onChange={(event) => setModel(event.target.value)}
        />

        <label htmlFor="input">Input</label>
        <textarea
          id="input"
          rows={5}
          value={input}
          onChange={(event) => setInput(event.target.value)}
        />

        <button onClick={runLlm} disabled={llmLoading}>
          {llmLoading ? "Running..." : "Send /v1/llm"}
        </button>

        {llmResult ? <pre>{JSON.stringify(llmResult, null, 2)}</pre> : null}
      </section>
    </main>
  );
}

export default App;
