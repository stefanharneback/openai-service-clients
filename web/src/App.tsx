import { useState } from "react";

import {
  getHealth,
  sendLlm,
  streamLlm,
  postWhisper,
  getUsage,
  getAdminUsage,
  type HealthResponse,
  type UsageResponse,
  type AdminUsageResponse,
} from "./api/client";

const defaultBaseUrl = "http://localhost:3000";

function App() {
  const [baseUrl, setBaseUrl] = useState(defaultBaseUrl);
  const [result, setResult] = useState<HealthResponse | null>(null);
  const [llmResult, setLlmResult] = useState<unknown>(null);
  const [llmStreamResult, setLlmStreamResult] = useState("");
  const [whisperResult, setWhisperResult] = useState<unknown>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [llmLoading, setLlmLoading] = useState(false);
  const [llmStreaming, setLlmStreaming] = useState(false);
  const [whisperLoading, setWhisperLoading] = useState(false);
  const [usageResult, setUsageResult] = useState<UsageResponse | null>(null);
  const [adminUsageResult, setAdminUsageResult] = useState<AdminUsageResponse | null>(null);
  const [usageLoading, setUsageLoading] = useState(false);
  const [adminUsageLoading, setAdminUsageLoading] = useState(false);
  const [usageLimit, setUsageLimit] = useState(20);
  const [usageOffset, setUsageOffset] = useState(0);
  const [adminKey, setAdminKey] = useState("");
  const [adminLimit, setAdminLimit] = useState(20);
  const [adminOffset, setAdminOffset] = useState(0);
  const [apiKey, setApiKey] = useState("");
  const [model, setModel] = useState("gpt-5.4-mini");
  const [input, setInput] = useState("Summarize the value of API gateways in three bullets.");
  const [whisperModel, setWhisperModel] = useState("whisper-1");
  const [whisperFile, setWhisperFile] = useState<File | null>(null);

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
      setLlmStreamResult("");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
      setLlmResult(null);
    } finally {
      setLlmLoading(false);
    }
  };

  const runLlmStream = async () => {
    setLlmStreaming(true);
    setError(null);
    setLlmResult(null);
    setLlmStreamResult("Streaming output:\n\n");

    try {
      await streamLlm(
        baseUrl,
        apiKey,
        {
          model,
          input,
          stream: true,
        },
        (chunk) => {
          setLlmStreamResult((previous) => previous + chunk);
        },
      );
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setLlmStreaming(false);
    }
  };

  const runWhisper = async () => {
    if (!whisperFile) {
      setError("Please select an audio file.");
      return;
    }

    setWhisperLoading(true);
    setError(null);
    setWhisperResult(null);

    try {
      const payload = await postWhisper(baseUrl, apiKey, whisperModel, whisperFile);
      setWhisperResult(payload);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setWhisperLoading(false);
    }
  };

  const runUsage = async () => {
    setUsageLoading(true);
    setError(null);
    setUsageResult(null);

    try {
      const payload = await getUsage(baseUrl, apiKey, usageLimit, usageOffset);
      setUsageResult(payload);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setUsageLoading(false);
    }
  };

  const runAdminUsage = async () => {
    setAdminUsageLoading(true);
    setError(null);
    setAdminUsageResult(null);

    try {
      const payload = await getAdminUsage(baseUrl, adminKey, adminLimit, adminOffset);
      setAdminUsageResult(payload);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setAdminUsageLoading(false);
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

        <h2>LLM</h2>

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

        <button onClick={runLlmStream} disabled={llmStreaming}>
          {llmStreaming ? "Streaming..." : "Stream /v1/llm"}
        </button>

        {llmResult ? <pre>{JSON.stringify(llmResult, null, 2)}</pre> : null}
        {llmStreamResult ? <pre>{llmStreamResult}</pre> : null}

        <h2>Whisper</h2>

        <label htmlFor="whisper-model">Whisper model</label>
        <input
          id="whisper-model"
          value={whisperModel}
          onChange={(event) => setWhisperModel(event.target.value)}
        />

        <label htmlFor="whisper-file">Audio file</label>
        <input
          id="whisper-file"
          type="file"
          accept="audio/*,.mp3,.mp4,.wav,.m4a,.webm,.ogg,.flac,.aac"
          onChange={(event) => setWhisperFile(event.target.files?.[0] ?? null)}
        />

        <button onClick={runWhisper} disabled={whisperLoading}>
          {whisperLoading ? "Transcribing..." : "Transcribe /v1/whisper"}
        </button>

        {whisperResult ? <pre>{JSON.stringify(whisperResult, null, 2)}</pre> : null}

        <h2>Usage</h2>

        <label htmlFor="usage-limit">Limit</label>
        <input
          id="usage-limit"
          type="number"
          min={1}
          max={100}
          value={usageLimit}
          onChange={(event) => setUsageLimit(Number(event.target.value))}
        />

        <label htmlFor="usage-offset">Offset</label>
        <input
          id="usage-offset"
          type="number"
          min={0}
          value={usageOffset}
          onChange={(event) => setUsageOffset(Number(event.target.value))}
        />

        <button onClick={runUsage} disabled={usageLoading}>
          {usageLoading ? "Loading..." : "Get /v1/usage"}
        </button>

        {usageResult ? <pre>{JSON.stringify(usageResult, null, 2)}</pre> : null}

        <h2>Admin Usage</h2>

        <label htmlFor="admin-key">Admin key</label>
        <input
          id="admin-key"
          type="password"
          value={adminKey}
          onChange={(event) => setAdminKey(event.target.value)}
        />

        <label htmlFor="admin-limit">Limit</label>
        <input
          id="admin-limit"
          type="number"
          min={1}
          max={100}
          value={adminLimit}
          onChange={(event) => setAdminLimit(Number(event.target.value))}
        />

        <label htmlFor="admin-offset">Offset</label>
        <input
          id="admin-offset"
          type="number"
          min={0}
          value={adminOffset}
          onChange={(event) => setAdminOffset(Number(event.target.value))}
        />

        <button onClick={runAdminUsage} disabled={adminUsageLoading}>
          {adminUsageLoading ? "Loading..." : "Get /v1/admin/usage"}
        </button>

        {adminUsageResult ? <pre>{JSON.stringify(adminUsageResult, null, 2)}</pre> : null}
      </section>
    </main>
  );
}

export default App;
