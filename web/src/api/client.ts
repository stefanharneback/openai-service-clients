export type HealthResponse = {
  ok: boolean;
  service: string;
  requestId: string;
  now: string;
};

export const getHealth = async (baseUrl: string): Promise<HealthResponse> => {
  const response = await fetch(`${baseUrl}/health`);
  if (!response.ok) {
    throw new Error(`Health request failed with status ${response.status}`);
  }

  return (await response.json()) as HealthResponse;
};
