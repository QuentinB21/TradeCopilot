export function toNumberInput(value: number | null | undefined): string {
  return value === null || value === undefined ? "" : String(value);
}

export function parseDecimalInput(value: string, fallback = 0): number {
  const normalized = value.trim().replace(",", ".");
  if (normalized === "" || normalized === "-" || normalized === "." || normalized === "-.") {
    return fallback;
  }

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : fallback;
}

export function parseNullableDecimalInput(value: string): number | null {
  return value.trim() === "" ? null : parseDecimalInput(value);
}
