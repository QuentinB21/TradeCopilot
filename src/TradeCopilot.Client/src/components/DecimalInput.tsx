import type { ChangeEvent } from "react";

type DecimalInputProps = {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  min?: number;
  max?: number;
  step?: string | number;
};

const decimalPattern = /^-?\d*(?:[.,]\d*)?$/;

export function DecimalInput({ value, onChange, placeholder = "0", ...props }: DecimalInputProps) {
  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const nextValue = event.target.value;
    if (nextValue === "" || decimalPattern.test(nextValue)) {
      onChange(nextValue);
    }
  }

  return (
    <input
      {...props}
      inputMode="decimal"
      placeholder={placeholder}
      type="text"
      value={value}
      onChange={handleChange}
    />
  );
}
