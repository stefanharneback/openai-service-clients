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
};

export const getHealth = async (baseUrl: string): Promise<HealthResponse> => {
  const response = await fetch(`${baseUrl}/health`);
  if (!response.ok) {
    throw new Error(`Health request failed with status ${response.status}`);
  }

  return (await response.json()) as HealthResponse;
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

const parseSseDataLine = (line: string): string | null => {
  if (!line.startsWith("data:")) {
    return null;
  }

  return line.slice(5).trim();
};

const extractDisplayChunk = (data: string): string => {
  if (data === "[DONE]") {
    return "\n\n[done]";
  }

  try {
    const payload = JSON.parse(data) as { type?: string; delta?: string };
    if (typeof payload.delta === "string") {
      return payload.delta;
    }

    if (payload.type === "response.completed") {
      return "\n\n[completed]";
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
