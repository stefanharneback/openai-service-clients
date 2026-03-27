export type HealthResponse = {
  ok: boolean;
  service: string;
  requestId: string;
  now: string;
};

export type LlmRequest = {
  model: string;
  input: string;
  stream?: boolean;
  [key: string]: unknown;
};

export const getHealth = async (baseUrl: string): Promise<HealthResponse> => {
  const response = await fetch(`${baseUrl}/health`);
  if (!response.ok) {
    throw new Error(`Health request failed with status ${response.status}`);
  }

  return (await response.json()) as HealthResponse;
};

export type ModelsResponse = {
  models: string[];
  unrestricted: boolean;
};

export const getModels = async (baseUrl: string, apiKey: string): Promise<ModelsResponse> => {
  const response = await fetch(`${baseUrl}/v1/models`, {
    headers: { authorization: `Bearer ${apiKey}` },
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`Models request failed (${response.status}): ${responseText}`);
  }

  return JSON.parse(responseText) as ModelsResponse;
};

export const sendLlm = async (
  baseUrl: string,
  apiKey: string,
  payload: LlmRequest,
): Promise<unknown> => {
  const response = await fetch(`${baseUrl}/v1/llm`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      authorization: `Bearer ${apiKey}`,
    },
    body: JSON.stringify(payload),
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`LLM request failed (${response.status}): ${responseText}`);
  }

  try {
    return JSON.parse(responseText) as unknown;
  } catch {
    return responseText;
  }
};

type StreamChunkHandler = (chunk: string) => void;

export const parseSseDataLine = (line: string): string | null => {
  if (!line.startsWith("data:")) {
    return null;
  }

  return line.slice(5).trim();
};

type ResponseEventPayload = {
  type?: string;
  delta?: string;
  item?: {
    content?: Array<{
      type?: string;
      text?: string;
    }>;
  };
  error?: {
    code?: string;
    message?: string;
  };
  response?: {
    status_details?: {
      reason?: string;
    };
  };
};

const extractOutputItemText = (payload: ResponseEventPayload): string => {
  const content = payload.item?.content;
  if (!Array.isArray(content)) {
    return "";
  }

  const text = content
    .filter((item) => item?.type === "output_text" && typeof item.text === "string")
    .map((item) => item.text?.trim() ?? "")
    .filter((item) => item.length > 0);

  return text.join("\n");
};

export const extractDisplayChunk = (data: string): string => {
  if (data === "[DONE]") {
    return "\n\n[done]";
  }

  try {
    const payload = JSON.parse(data) as ResponseEventPayload;
    if (typeof payload.delta === "string") {
      return payload.delta;
    }

    if (payload.type === "response.output_item.done") {
      return extractOutputItemText(payload);
    }

    if (payload.type === "response.completed") {
      return "\n\n[completed]";
    }

    if (payload.type === "response.failed") {
      const errorCode = payload.error?.code ?? "unknown_error";
      const errorMessage = payload.error?.message ?? "Streaming response failed.";
      return `\n\n[failed: ${errorCode}] ${errorMessage}`;
    }

    if (payload.type === "response.incomplete") {
      const reason = payload.response?.status_details?.reason ?? "unknown";
      return `\n\n[incomplete: ${reason}]`;
    }

    return "";
  } catch {
    return `${data}\n`;
  }
};

export const streamLlm = async (
  baseUrl: string,
  apiKey: string,
  payload: LlmRequest,
  onChunk: StreamChunkHandler,
): Promise<void> => {
  const response = await fetch(`${baseUrl}/v1/llm`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      accept: "text/event-stream",
      authorization: `Bearer ${apiKey}`,
    },
    body: JSON.stringify({
      ...payload,
      stream: true,
    }),
  });

  if (!response.ok) {
    const responseText = await response.text();
    throw new Error(`LLM stream request failed (${response.status}): ${responseText}`);
  }

  if (!response.body) {
    throw new Error("Streaming response body was empty.");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split(/\r?\n/);
    buffer = lines.pop() ?? "";

    for (const line of lines) {
      const data = parseSseDataLine(line);
      if (!data) {
        continue;
      }

      const chunk = extractDisplayChunk(data);
      if (chunk.length > 0) {
        onChunk(chunk);
      }
    }
  }

  if (buffer.trim().length > 0) {
    const data = parseSseDataLine(buffer.trim());
    if (data) {
      const chunk = extractDisplayChunk(data);
      if (chunk.length > 0) {
        onChunk(chunk);
      }
    }
  }
};

export const postWhisper = async (
  baseUrl: string,
  apiKey: string,
  model: string,
  file: File,
): Promise<unknown> => {
  const formData = new FormData();
  formData.append("model", model);
  formData.append("file", file, file.name);

  const response = await fetch(`${baseUrl}/v1/whisper`, {
    method: "POST",
    headers: {
      authorization: `Bearer ${apiKey}`,
    },
    body: formData,
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`Whisper request failed (${response.status}): ${responseText}`);
  }

  try {
    return JSON.parse(responseText) as unknown;
  } catch {
    return responseText;
  }
};

export type UsageItem = {
  id: string;
  created_at: string;
  endpoint: string;
  model: string;
  http_status: number;
  duration_ms: number;
  input_tokens?: number | null;
  output_tokens?: number | null;
  total_tokens?: number | null;
  total_cost_usd?: number | null;
};

export type AdminUsageItem = UsageItem & {
  client_id?: string | null;
};

export type UsageResponse = {
  clientId: string;
  items: UsageItem[];
  limit: number;
  offset: number;
};

export type AdminUsageResponse = {
  items: AdminUsageItem[];
  limit: number;
  offset: number;
};

export const getUsage = async (
  baseUrl: string,
  apiKey: string,
  limit = 20,
  offset = 0,
): Promise<UsageResponse> => {
  const url = `${baseUrl}/v1/usage?limit=${limit}&offset=${offset}`;
  const response = await fetch(url, {
    headers: { authorization: `Bearer ${apiKey}` },
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`Usage request failed (${response.status}): ${responseText}`);
  }

  return JSON.parse(responseText) as UsageResponse;
};

export const getAdminUsage = async (
  baseUrl: string,
  adminKey: string,
  limit = 20,
  offset = 0,
): Promise<AdminUsageResponse> => {
  const url = `${baseUrl}/v1/admin/usage?limit=${limit}&offset=${offset}`;
  const response = await fetch(url, {
    headers: { authorization: `Bearer ${adminKey}` },
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`Admin usage request failed (${response.status}): ${responseText}`);
  }

  return JSON.parse(responseText) as AdminUsageResponse;
};
