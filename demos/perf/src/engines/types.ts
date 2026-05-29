export interface EngineAdapter {
  name: string;
  baseUrl: string;
  buildInput(id: string): unknown;
  // `start` accepts the JSON-serializable input; returns the workflow id the engine
  // assigned. For engines where the caller chooses the id, just return the same one.
  start(input: unknown): Promise<string>;
  // Polls the engine's status endpoint and returns true iff the workflow is done.
  isComplete(id: string): Promise<boolean>;
}

export interface AdapterFactory {
  create(baseUrl: string): EngineAdapter;
}
