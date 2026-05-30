import { Controller, useFormContext } from 'react-hook-form';
import { TextField, FormControl, InputLabel, Select } from '@mui/material';
import { fieldSx, selectFormSx } from './styles';

// Thin wrappers that bind MUI inputs to React Hook Form via <Controller>.
// We use Controller (rather than register) uniformly because the form mixes
// TextFields, Selects, Autocompletes, radios and checkboxes — Controller keeps
// every binding correct and the field components declarative.

/**
 * RHF-bound MUI TextField. Surfaces the field's validation message as
 * helperText unless an explicit helperText is provided.
 */
export function ControlledTextField({ name, sx, helperText, ...props }) {
  const { control } = useFormContext();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <TextField
          {...field}
          value={field.value ?? ''}
          error={!!fieldState.error}
          helperText={fieldState.error?.message ?? helperText}
          sx={{ ...fieldSx, ...sx }}
          {...props}
        />
      )}
    />
  );
}

/**
 * RHF-bound MUI Select wrapped in a labelled FormControl.
 * `children` are the <MenuItem> options.
 */
export function ControlledSelect({ name, label, required, sx, selectProps, children, ...formControlProps }) {
  const { control } = useFormContext();
  const labelId = `${name}-label`;
  return (
    <FormControl fullWidth margin="normal" required={required} sx={{ ...selectFormSx, ...sx }} {...formControlProps}>
      <InputLabel id={labelId}>{label}</InputLabel>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select labelId={labelId} label={label} {...field} value={field.value ?? ''} {...selectProps}>
            {children}
          </Select>
        )}
      />
    </FormControl>
  );
}
