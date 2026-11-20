const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:7100';

export async function checkApiHealth(): Promise<{ status: string }> {
  const response = await fetch(`${API_BASE_URL}/health`);
  if (!response.ok) {
    throw new Error(`Health check failed: ${response.statusText}`);
  }
  return response.json();
}