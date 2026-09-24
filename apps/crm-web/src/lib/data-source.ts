export interface CrmDataSource {
  readonly kind: "bff" | "demo";
  getScreenData(screenId: string): Promise<unknown>;
  saveMutation(name: string, payload: unknown): Promise<void>;
}

export class BffCrmDataSource implements CrmDataSource {
  readonly kind = "bff" as const;

  constructor(private readonly baseUrl = process.env.NEXT_PUBLIC_CRM_BFF_URL ?? "http://localhost:5137") {}

  async getScreenData(screenId: string): Promise<unknown> {
    if (!this.baseUrl) throw new Error("CRM_BFF_NOT_CONFIGURED");
    const response = await fetch(`${this.baseUrl}/screens/${encodeURIComponent(screenId)}`, { credentials: "include" });
    if (!response.ok) throw new Error(`CRM_BFF_${response.status}`);
    return response.json();
  }

  async saveMutation(name: string, payload: unknown): Promise<void> {
    if (!this.baseUrl) throw new Error("CRM_BFF_NOT_CONFIGURED");
    const response = await fetch(`${this.baseUrl}/mutations/${encodeURIComponent(name)}`, {
      method: "POST",
      credentials: "include",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error(`CRM_BFF_${response.status}`);
  }
}

export class DemoCrmDataSource implements CrmDataSource {
  readonly kind = "demo" as const;

  constructor(private readonly snapshot: unknown) {}

  async getScreenData(screenId: string): Promise<unknown> {
    return { screenId, snapshot: this.snapshot };
  }

  async saveMutation(): Promise<void> {
    return Promise.resolve();
  }
}

export function createCrmDataSource(mode: string | undefined, snapshot?: unknown): CrmDataSource {
  return mode === "demo" ? new DemoCrmDataSource(snapshot) : new BffCrmDataSource();
}
