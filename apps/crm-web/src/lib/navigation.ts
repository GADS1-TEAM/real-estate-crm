export function buildCrmScreenUrl(pathname: string, screen: string, entity?: string | null): string {
  const query = new URLSearchParams({ screen });
  if (entity !== undefined && entity !== null) query.set("entity", entity);
  return `${pathname}?${query.toString()}`;
}
