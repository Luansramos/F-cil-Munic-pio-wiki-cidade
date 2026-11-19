export function gerarId(): string {
  const aleatorio = Math.random().toString(36).slice(2, 10);
  return `${Date.now().toString(36)}-${aleatorio}`;
}
