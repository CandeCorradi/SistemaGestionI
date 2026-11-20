'use client';

import {
  type InputHTMLAttributes,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react';

interface FieldShellProps {
  label: string;
  htmlFor: string;
  error?: string | null;
  required?: boolean;
  hint?: string;
  children: React.ReactNode;
}

function FieldShell({ label, htmlFor, error, required, hint, children }: FieldShellProps) {
  return (
    <div className="mb-3">
      <label htmlFor={htmlFor} className="form-label">
        {label}
        {required ? <span className="text-danger ms-1">*</span> : null}
      </label>
      {children}
      {hint && !error ? <div className="form-text">{hint}</div> : null}
      {error ? (
        <div className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      ) : null}
    </div>
  );
}

interface AppInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  label: string;
  name: string;
  error?: string | null;
  hint?: string;
}

/** Input de texto/email/etc. con label y mensaje de error (roadmap §17). */
export function AppInput({ label, name, error, hint, required, ...rest }: AppInputProps) {
  const id = `field-${name}`;
  return (
    <FieldShell label={label} htmlFor={id} error={error} required={required} hint={hint}>
      <input
        id={id}
        name={name}
        className={`form-control ${error ? 'is-invalid' : ''}`}
        required={required}
        aria-invalid={error ? true : undefined}
        {...rest}
      />
    </FieldShell>
  );
}

interface AppTextareaProps extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id'> {
  label: string;
  name: string;
  error?: string | null;
  hint?: string;
}

/** Textarea con label y mensaje de error. */
export function AppTextarea({ label, name, error, hint, required, ...rest }: AppTextareaProps) {
  const id = `field-${name}`;
  return (
    <FieldShell label={label} htmlFor={id} error={error} required={required} hint={hint}>
      <textarea
        id={id}
        name={name}
        className={`form-control ${error ? 'is-invalid' : ''}`}
        required={required}
        aria-invalid={error ? true : undefined}
        {...rest}
      />
    </FieldShell>
  );
}

interface SelectOption {
  value: string;
  label: string;
}

interface AppSelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> {
  label: string;
  name: string;
  options: SelectOption[];
  error?: string | null;
  placeholder?: string;
}

/** Select con label y opción por defecto. */
export function AppSelect({
  label,
  name,
  options,
  error,
  placeholder,
  required,
  ...rest
}: AppSelectProps) {
  const id = `field-${name}`;
  return (
    <FieldShell label={label} htmlFor={id} error={error} required={required}>
      <select
        id={id}
        name={name}
        className={`form-select ${error ? 'is-invalid' : ''}`}
        required={required}
        aria-invalid={error ? true : undefined}
        {...rest}
      >
        {placeholder ? <option value="">{placeholder}</option> : null}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FieldShell>
  );
}
