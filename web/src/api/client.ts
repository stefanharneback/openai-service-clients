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
