import React from 'react';

// ============== Contexts ==============
const RadioGroupContext = React.createContext({});
const AccordionContext = React.createContext({ expanded: false, toggle: () => {} });

// ============== Layout Components ==============

export const Box = React.forwardRef(({
  children, component: Tag = 'div', className, sx,
  display, flexDirection, flexGrow, flexShrink, flex, alignItems, justifyContent,
  gap, p, px, py, m, mx, my, mt, mb, ml, mr, pt, pb, pl, pr, width, height,
  minWidth, maxWidth, minHeight, maxHeight, overflow, position, top, left, right, bottom,
  textAlign, bgcolor, border, borderRadius, borderColor, boxShadow, zIndex,
  ...rest
}, ref) => (
  <Tag ref={ref} className={`MuiBox-root ${className || ''}`.trim()} {...rest}>{children}</Tag>
));
Box.displayName = 'Box';

export const Container = React.forwardRef(({ children, className, maxWidth, fixed, disableGutters, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiContainer-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
Container.displayName = 'Container';

export const Paper = React.forwardRef(({ children, className, elevation, square, variant, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiPaper-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
Paper.displayName = 'Paper';

export const Stack = React.forwardRef(({ children, className, direction, spacing, alignItems, justifyContent, divider, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiStack-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
Stack.displayName = 'Stack';

export const Grid = React.forwardRef(({
  children, container, item, className, xs, sm, md, lg, xl,
  spacing, direction, alignItems, justifyContent, wrap, sx, ...rest
}, ref) => {
  const classes = [
    container && 'MuiGrid-container',
    item && 'MuiGrid-item',
    className
  ].filter(Boolean).join(' ');
  return <div ref={ref} className={classes} {...rest}>{children}</div>;
});
Grid.displayName = 'Grid';

export const AppBar = React.forwardRef(({ children, className, position, color, elevation, sx, ...rest }, ref) => (
  <header ref={ref} className={`MuiAppBar-root ${className || ''}`.trim()} {...rest}>{children}</header>
));
AppBar.displayName = 'AppBar';

export const Toolbar = React.forwardRef(({ children, className, variant, disableGutters, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiToolbar-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
Toolbar.displayName = 'Toolbar';

export const Drawer = React.forwardRef(({ children, open, onClose, anchor, variant, className, sx, ...rest }, ref) => {
  if (!open && variant !== 'permanent') return null;
  return <div ref={ref} className={`MuiDrawer-root ${className || ''}`.trim()} {...rest}>{children}</div>;
});
Drawer.displayName = 'Drawer';

export const CssBaseline = () => null;

export const Divider = React.forwardRef(({ className, sx, orientation, flexItem, ...rest }, ref) => (
  <hr ref={ref} className={`MuiDivider-root ${className || ''}`.trim()} {...rest} />
));
Divider.displayName = 'Divider';

export const Collapse = React.forwardRef(({ children, in: open, timeout, sx, ...rest }, ref) => {
  if (!open) return null;
  return <div ref={ref} {...rest}>{children}</div>;
});
Collapse.displayName = 'Collapse';

// ============== Typography ==============

const variantToTag = {
  h1: 'h1', h2: 'h2', h3: 'h3', h4: 'h4', h5: 'h5', h6: 'h6',
  subtitle1: 'p', subtitle2: 'p', body1: 'p', body2: 'p',
  caption: 'span', overline: 'span', button: 'span',
};

export const Typography = React.forwardRef(({
  children, variant, component, className, sx, color, align, gutterBottom, noWrap, paragraph,
  ...rest
}, ref) => {
  const Tag = component || variantToTag[variant] || 'p';
  return <Tag ref={ref} className={`MuiTypography-root ${className || ''}`.trim()} {...rest}>{children}</Tag>;
});
Typography.displayName = 'Typography';

// ============== Buttons ==============

export const Button = React.forwardRef(({
  children, variant, color, size, disabled, onClick, type, startIcon, endIcon,
  fullWidth, disableElevation, disableRipple, href, component: Component, sx, className,
  ...rest
}, ref) => {
  const classes = [
    'MuiButton-root',
    variant && `MuiButton-${variant}`,
    className
  ].filter(Boolean).join(' ');
  if (Component) {
    return <Component ref={ref} className={classes} disabled={disabled} onClick={onClick} {...rest}>{startIcon}{children}{endIcon}</Component>;
  }
  return (
    <button ref={ref} className={classes} disabled={disabled} onClick={onClick} type={type || 'button'} {...rest}>
      {startIcon}{children}{endIcon}
    </button>
  );
});
Button.displayName = 'Button';

export const IconButton = React.forwardRef(({
  children, disabled, onClick, size, color, edge, sx, className, component, ...rest
}, ref) => (
  <button ref={ref} className={`MuiIconButton-root ${className || ''}`.trim()} disabled={disabled} onClick={onClick} {...rest}>
    {children}
  </button>
));
IconButton.displayName = 'IconButton';

export const ButtonGroup = React.forwardRef(({ children, className, variant, color, size, orientation, sx, ...rest }, ref) => (
  <div ref={ref} role="group" className={`MuiButtonGroup-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
ButtonGroup.displayName = 'ButtonGroup';

export const ToggleButton = React.forwardRef(({
  children, value, selected, onChange, disabled, size, color, className, sx, ...rest
}, ref) => (
  <button ref={ref} className={`MuiToggleButton-root ${selected ? 'Mui-selected' : ''} ${className || ''}`.trim()} disabled={disabled} onClick={onChange} value={value} {...rest}>
    {children}
  </button>
));
ToggleButton.displayName = 'ToggleButton';

export const ToggleButtonGroup = React.forwardRef(({
  children, value, onChange, exclusive, orientation, size, className, sx, ...rest
}, ref) => (
  <div ref={ref} role="group" className={`MuiToggleButtonGroup-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
ToggleButtonGroup.displayName = 'ToggleButtonGroup';

export const Fab = React.forwardRef(({ children, onClick, color, size, variant, disabled, className, sx, ...rest }, ref) => (
  <button ref={ref} className={`MuiFab-root ${className || ''}`.trim()} onClick={onClick} disabled={disabled} {...rest}>{children}</button>
));
Fab.displayName = 'Fab';

// ============== Form Components ==============

export const TextField = React.forwardRef(({
  label, placeholder, multiline, rows, type, required, disabled, value, defaultValue,
  onChange, onKeyDown, onBlur, onFocus, InputProps, inputProps, InputLabelProps,
  error, helperText, fullWidth, variant, size, sx, className, id: propId,
  select, children, autoFocus, name, margin, minRows, maxRows, ...rest
}, ref) => {
  const id = propId || `tf-${(label || placeholder || Math.random().toString(36).slice(2)).replace(/\s+/g, '-').toLowerCase()}`;
  return (
    <div className={`MuiTextField-root ${className || ''}`.trim()}>
      {label && <label htmlFor={id}>{label}</label>}
      {select ? (
        <select ref={ref} id={id} value={value} onChange={onChange} disabled={disabled} {...inputProps}>
          {children}
        </select>
      ) : multiline ? (
        <textarea
          ref={ref}
          id={id}
          name={name}
          placeholder={placeholder}
          required={required}
          disabled={disabled}
          value={value}
          defaultValue={defaultValue}
          onChange={onChange}
          onKeyDown={onKeyDown}
          onBlur={onBlur}
          onFocus={onFocus}
          rows={rows}
          autoFocus={autoFocus}
          {...inputProps}
        />
      ) : (
        <input
          ref={ref}
          id={id}
          name={name}
          type={type || 'text'}
          placeholder={placeholder}
          required={required}
          disabled={disabled}
          value={value}
          defaultValue={defaultValue}
          onChange={onChange}
          onKeyDown={onKeyDown}
          onBlur={onBlur}
          onFocus={onFocus}
          autoFocus={autoFocus}
          {...inputProps}
        />
      )}
      {helperText && <span className="MuiFormHelperText-root">{helperText}</span>}
    </div>
  );
});
TextField.displayName = 'TextField';

export const FormControl = React.forwardRef(({ children, className, fullWidth, error, required, variant, size, margin, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiFormControl-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
FormControl.displayName = 'FormControl';

export const FormLabel = React.forwardRef(({ children, className, error, required, sx, ...rest }, ref) => (
  <label ref={ref} className={`MuiFormLabel-root ${className || ''}`.trim()} {...rest}>{children}</label>
));
FormLabel.displayName = 'FormLabel';

export const FormGroup = React.forwardRef(({ children, className, row, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiFormGroup-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
FormGroup.displayName = 'FormGroup';

export const FormControlLabel = React.forwardRef(({ control, label, value, disabled, className, sx, ...rest }, ref) => (
  <label ref={ref} className={`MuiFormControlLabel-root ${className || ''}`.trim()} {...rest}>
    {React.cloneElement(control, { value, disabled })}
    <span>{label}</span>
  </label>
));
FormControlLabel.displayName = 'FormControlLabel';

export const RadioGroup = React.forwardRef(({ children, value, onChange, name, row, className, sx, ...rest }, ref) => (
  <RadioGroupContext.Provider value={{ value, onChange, name }}>
    <div ref={ref} role="radiogroup" className={`MuiRadioGroup-root ${className || ''}`.trim()} {...rest}>{children}</div>
  </RadioGroupContext.Provider>
));
RadioGroup.displayName = 'RadioGroup';

export const Radio = React.forwardRef(({ value: radioValue, checked: controlledChecked, disabled, className, sx, size, color, ...rest }, ref) => {
  const ctx = React.useContext(RadioGroupContext);
  const checked = controlledChecked !== undefined ? controlledChecked : (ctx.value === radioValue);
  return (
    <input
      ref={ref}
      type="radio"
      name={ctx.name}
      value={radioValue}
      checked={checked}
      disabled={disabled}
      onChange={ctx.onChange}
      {...rest}
    />
  );
});
Radio.displayName = 'Radio';

export const Checkbox = React.forwardRef(({ checked, defaultChecked, disabled, onChange, indeterminate, color, size, sx, className, icon, checkedIcon, ...rest }, ref) => (
  <input
    ref={ref}
    type="checkbox"
    checked={checked}
    defaultChecked={defaultChecked}
    disabled={disabled}
    onChange={onChange}
    {...rest}
  />
));
Checkbox.displayName = 'Checkbox';

export const Switch = React.forwardRef(({ checked, defaultChecked, disabled, onChange, color, size, sx, ...rest }, ref) => (
  <input ref={ref} type="checkbox" role="switch" checked={checked} defaultChecked={defaultChecked} disabled={disabled} onChange={onChange} {...rest} />
));
Switch.displayName = 'Switch';

export const Select = React.forwardRef(({ children, value, onChange, label, labelId, displayEmpty, variant, fullWidth, className, sx, multiple, renderValue, input, MenuProps, ...rest }, ref) => (
  <div ref={ref} className={`MuiSelect-root ${className || ''}`.trim()}>
    <select value={value} onChange={e => onChange?.({ target: { value: e.target.value } })} multiple={multiple} {...rest}>
      {children}
    </select>
  </div>
));
Select.displayName = 'Select';

export const MenuItem = React.forwardRef(({ children, value, onClick, selected, disabled, className, sx, ...rest }, ref) => (
  <li ref={ref} role="menuitem" data-value={value} onClick={onClick} className={`MuiMenuItem-root ${className || ''}`.trim()} {...rest}>{children}</li>
));
MenuItem.displayName = 'MenuItem';

export const InputLabel = React.forwardRef(({ children, className, htmlFor, shrink, sx, ...rest }, ref) => (
  <label ref={ref} htmlFor={htmlFor} className={`MuiInputLabel-root ${className || ''}`.trim()} {...rest}>{children}</label>
));
InputLabel.displayName = 'InputLabel';

export const InputAdornment = React.forwardRef(({ children, position, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiInputAdornment-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
InputAdornment.displayName = 'InputAdornment';

export const OutlinedInput = React.forwardRef(({ className, sx, label, ...rest }, ref) => (
  <input ref={ref} className={`MuiOutlinedInput-root ${className || ''}`.trim()} {...rest} />
));
OutlinedInput.displayName = 'OutlinedInput';

export const Autocomplete = React.forwardRef(({
  renderInput, renderTags, options, value, onChange, multiple, freeSolo, filterSelectedOptions,
  getOptionLabel, isOptionEqualToValue, loading, loadingText, noOptionsText,
  onInputChange, inputValue, className, sx, disablePortal, ListboxProps, disabled,
  ...rest
}, ref) => {
  const [internalValue, setInternalValue] = React.useState(value || (multiple ? [] : null));
  React.useEffect(() => { setInternalValue(value || (multiple ? [] : null)); }, [value, multiple]);

  const handleKeyDown = (e) => {
    if (freeSolo && multiple && e.key === 'Enter' && e.target.value.trim()) {
      e.preventDefault();
      const newVal = [...(internalValue || []), e.target.value.trim()];
      setInternalValue(newVal);
      if (onChange) onChange(e, newVal);
      e.target.value = '';
    }
  };

  const getTagProps = ({ index }) => ({
    onDelete: () => {
      const newVal = (internalValue || []).filter((_, i) => i !== index);
      setInternalValue(newVal);
      if (onChange) onChange({}, newVal);
    }
  });

  const tags = multiple && renderTags && (internalValue || []).length > 0
    ? renderTags(internalValue, getTagProps)
    : null;

  const inputEl = renderInput ? renderInput({
    inputProps: { onKeyDown: handleKeyDown },
    InputProps: {},
    InputLabelProps: {}
  }) : null;

  return (
    <div ref={ref} className={`MuiAutocomplete-root ${className || ''}`.trim()}>
      {tags}
      {inputEl}
    </div>
  );
});
Autocomplete.displayName = 'Autocomplete';

export const Slider = React.forwardRef(({ value, onChange, min, max, step, marks, valueLabelDisplay, className, sx, ...rest }, ref) => (
  <input ref={ref} type="range" value={value} onChange={onChange} min={min} max={max} step={step} {...rest} />
));
Slider.displayName = 'Slider';

// ============== Data Display ==============

export const Card = React.forwardRef(({ children, className, raised, elevation, variant, sx, onClick, ...rest }, ref) => (
  <div ref={ref} className={`MuiCard-root ${className || ''}`.trim()} onClick={onClick} {...rest}>{children}</div>
));
Card.displayName = 'Card';

export const CardContent = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiCardContent-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
CardContent.displayName = 'CardContent';

export const CardMedia = React.forwardRef(({ component, src, image, alt, children, className, sx, height, ...rest }, ref) => {
  const imgSrc = src || image;
  if (component === 'img' || (!children && imgSrc)) {
    return <img ref={ref} src={imgSrc} alt={alt} className={`MuiCardMedia-root ${className || ''}`.trim()} {...rest} />;
  }
  return <div ref={ref} className={`MuiCardMedia-root ${className || ''}`.trim()} style={imgSrc ? { backgroundImage: `url(${imgSrc})` } : undefined} {...rest}>{children}</div>;
});
CardMedia.displayName = 'CardMedia';

export const CardActions = React.forwardRef(({ children, className, disableSpacing, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiCardActions-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
CardActions.displayName = 'CardActions';

export const CardActionArea = React.forwardRef(({ children, className, onClick, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiCardActionArea-root ${className || ''}`.trim()} onClick={onClick} {...rest}>{children}</div>
));
CardActionArea.displayName = 'CardActionArea';

export const Chip = React.forwardRef(({ label, children, onDelete, onClick, icon, avatar, variant, color, size, className, sx, deleteIcon, ...rest }, ref) => (
  <span ref={ref} className={`MuiChip-root ${className || ''}`.trim()} onClick={onClick} {...rest}>
    {icon}
    {label || children}
    {onDelete && <button onClick={onDelete} aria-label="delete">{deleteIcon || '×'}</button>}
  </span>
));
Chip.displayName = 'Chip';

export const Badge = React.forwardRef(({ children, badgeContent, color, variant, invisible, overlap, anchorOrigin, className, sx, ...rest }, ref) => (
  <span ref={ref} className={`MuiBadge-root ${className || ''}`.trim()} {...rest}>
    {children}
    {!invisible && badgeContent != null && <span className="MuiBadge-badge">{badgeContent}</span>}
  </span>
));
Badge.displayName = 'Badge';

export const Rating = React.forwardRef(({ value, defaultValue, precision, readOnly, disabled, onChange, size, className, sx, ...rest }, ref) => (
  <span ref={ref} className={`MuiRating-root ${className || ''}`.trim()} role="img" aria-label={`${value || defaultValue || 0} Stars`} {...rest}>
    {'★'.repeat(Math.round(value || defaultValue || 0))}
  </span>
));
Rating.displayName = 'Rating';

export const Tooltip = React.forwardRef(({ children, title, placement, arrow, className, sx, ...rest }, ref) => {
  // Clone child to add aria-label from title, matching real MUI Tooltip behavior
  const child = React.isValidElement(children)
    ? React.cloneElement(children, { 'aria-label': title })
    : children;
  return <span ref={ref}>{child}</span>;
});
Tooltip.displayName = 'Tooltip';

export const Skeleton = React.forwardRef(({ variant, width, height, animation, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiSkeleton-root ${className || ''}`.trim()} {...rest} />
));
Skeleton.displayName = 'Skeleton';

// ============== Lists ==============

export const List = React.forwardRef(({ children, className, dense, disablePadding, subheader, sx, ...rest }, ref) => (
  <ul ref={ref} className={`MuiList-root ${className || ''}`.trim()} {...rest}>
    {subheader}
    {children}
  </ul>
));
List.displayName = 'List';

export const ListItem = React.forwardRef(({ children, className, button, divider, disablePadding, disableGutters, alignItems, secondaryAction, sx, ...rest }, ref) => (
  <li ref={ref} className={`MuiListItem-root ${className || ''}`.trim()} {...rest}>
    {children}
    {secondaryAction}
  </li>
));
ListItem.displayName = 'ListItem';

export const ListItemText = React.forwardRef(({ primary, secondary, className, sx, primaryTypographyProps, secondaryTypographyProps, ...rest }, ref) => (
  <div ref={ref} className={`MuiListItemText-root ${className || ''}`.trim()} {...rest}>
    {primary && <span className="MuiListItemText-primary">{primary}</span>}
    {secondary && <span className="MuiListItemText-secondary">{secondary}</span>}
  </div>
));
ListItemText.displayName = 'ListItemText';

export const ListItemIcon = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiListItemIcon-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
ListItemIcon.displayName = 'ListItemIcon';

export const ListItemButton = React.forwardRef(({ children, className, onClick, selected, disabled, sx, ...rest }, ref) => (
  <li ref={ref} role="button" className={`MuiListItemButton-root ${className || ''}`.trim()} onClick={onClick} {...rest}>{children}</li>
));
ListItemButton.displayName = 'ListItemButton';

export const ListItemSecondaryAction = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiListItemSecondaryAction-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
ListItemSecondaryAction.displayName = 'ListItemSecondaryAction';

export const ListSubheader = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <li ref={ref} className={`MuiListSubheader-root ${className || ''}`.trim()} {...rest}>{children}</li>
));
ListSubheader.displayName = 'ListSubheader';

// ============== Tables ==============

export const Table = React.forwardRef(({ children, className, size, stickyHeader, sx, ...rest }, ref) => (
  <table ref={ref} className={`MuiTable-root ${className || ''}`.trim()} {...rest}>{children}</table>
));
Table.displayName = 'Table';

export const TableHead = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <thead ref={ref} className={`MuiTableHead-root ${className || ''}`.trim()} {...rest}>{children}</thead>
));
TableHead.displayName = 'TableHead';

export const TableBody = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <tbody ref={ref} className={`MuiTableBody-root ${className || ''}`.trim()} {...rest}>{children}</tbody>
));
TableBody.displayName = 'TableBody';

export const TableRow = React.forwardRef(({ children, className, hover, selected, sx, ...rest }, ref) => (
  <tr ref={ref} className={`MuiTableRow-root ${className || ''}`.trim()} {...rest}>{children}</tr>
));
TableRow.displayName = 'TableRow';

export const TableCell = React.forwardRef(({ children, className, align, padding, size, variant, sortDirection, sx, ...rest }, ref) => (
  <td ref={ref} className={`MuiTableCell-root ${className || ''}`.trim()} {...rest}>{children}</td>
));
TableCell.displayName = 'TableCell';

export const TableContainer = React.forwardRef(({ children, className, component: Tag = 'div', sx, ...rest }, ref) => (
  <Tag ref={ref} className={`MuiTableContainer-root ${className || ''}`.trim()} {...rest}>{children}</Tag>
));
TableContainer.displayName = 'TableContainer';

// ============== Feedback ==============

export const CircularProgress = React.forwardRef(({ size, thickness, variant, value, color, className, sx, ...rest }, ref) => (
  <div ref={ref} role="progressbar" className={`MuiCircularProgress-root ${className || ''}`.trim()} {...rest} />
));
CircularProgress.displayName = 'CircularProgress';

export const LinearProgress = React.forwardRef(({ variant, value, valueBuffer, color, className, sx, ...rest }, ref) => (
  <div ref={ref} role="progressbar" className={`MuiLinearProgress-root ${className || ''}`.trim()} {...rest} />
));
LinearProgress.displayName = 'LinearProgress';

export const Alert = React.forwardRef(({ children, severity, variant, action, onClose, icon, className, sx, ...rest }, ref) => (
  <div ref={ref} role="alert" className={`MuiAlert-root ${className || ''}`.trim()} {...rest}>
    {children}
  </div>
));
Alert.displayName = 'Alert';

export const AlertTitle = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiAlertTitle-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
AlertTitle.displayName = 'AlertTitle';

export const Snackbar = React.forwardRef(({ children, open, onClose, autoHideDuration, message, action, anchorOrigin, className, sx, ...rest }, ref) => {
  if (!open) return null;
  return (
    <div ref={ref} className={`MuiSnackbar-root ${className || ''}`.trim()} {...rest}>
      {message && <div>{message}</div>}
      {children}
    </div>
  );
});
Snackbar.displayName = 'Snackbar';

// ============== Dialog ==============

export const Dialog = React.forwardRef(({ children, open, onClose, maxWidth, fullWidth, fullScreen, className, sx, ...rest }, ref) => {
  if (!open) return null;
  return <div ref={ref} role="dialog" className={`MuiDialog-root ${className || ''}`.trim()} {...rest}>{children}</div>;
});
Dialog.displayName = 'Dialog';

export const DialogTitle = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiDialogTitle-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
DialogTitle.displayName = 'DialogTitle';

export const DialogContent = React.forwardRef(({ children, className, dividers, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiDialogContent-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
DialogContent.displayName = 'DialogContent';

export const DialogContentText = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <p ref={ref} className={`MuiDialogContentText-root ${className || ''}`.trim()} {...rest}>{children}</p>
));
DialogContentText.displayName = 'DialogContentText';

export const DialogActions = React.forwardRef(({ children, className, sx, ...rest }, ref) => (
  <div ref={ref} className={`MuiDialogActions-root ${className || ''}`.trim()} {...rest}>{children}</div>
));
DialogActions.displayName = 'DialogActions';

// ============== Navigation ==============

export const Menu = React.forwardRef(({ children, open, onClose, anchorEl, anchorOrigin, transformOrigin, className, sx, ...rest }, ref) => {
  if (!open) return null;
  return <div ref={ref} role="menu" className={`MuiMenu-root ${className || ''}`.trim()} {...rest}>{children}</div>;
});
Menu.displayName = 'Menu';

export const Link = React.forwardRef(({ children, component: Component, href, to, underline, color, variant, className, sx, ...rest }, ref) => {
  if (Component) {
    return <Component ref={ref} to={to} href={href} className={`MuiLink-root ${className || ''}`.trim()} {...rest}>{children}</Component>;
  }
  return <a ref={ref} href={href || to} className={`MuiLink-root ${className || ''}`.trim()} {...rest}>{children}</a>;
});
Link.displayName = 'Link';

// ============== Accordion ==============

export const Accordion = React.forwardRef(({
  children, expanded: controlledExpanded, defaultExpanded = false, onChange, className, sx,
  disableGutters, elevation, square, TransitionProps, ...rest
}, ref) => {
  const [internalExpanded, setInternalExpanded] = React.useState(defaultExpanded);
  const expanded = controlledExpanded !== undefined ? controlledExpanded : internalExpanded;
  const toggle = () => {
    const newVal = !expanded;
    setInternalExpanded(newVal);
    onChange?.(null, newVal);
  };
  return (
    <AccordionContext.Provider value={{ expanded, toggle }}>
      <div ref={ref} className={`MuiAccordion-root ${className || ''}`.trim()} {...rest}>{children}</div>
    </AccordionContext.Provider>
  );
});
Accordion.displayName = 'Accordion';

export const AccordionSummary = React.forwardRef(({ children, expandIcon, className, sx, ...rest }, ref) => {
  const { toggle } = React.useContext(AccordionContext);
  return (
    <div ref={ref} role="button" onClick={toggle} className={`MuiAccordionSummary-root ${className || ''}`.trim()} {...rest}>
      {children}
      {expandIcon}
    </div>
  );
});
AccordionSummary.displayName = 'AccordionSummary';

export const AccordionDetails = React.forwardRef(({ children, className, sx, ...rest }, ref) => {
  const { expanded } = React.useContext(AccordionContext);
  if (!expanded) return null;
  return <div ref={ref} className={`MuiAccordionDetails-root ${className || ''}`.trim()} {...rest}>{children}</div>;
});
AccordionDetails.displayName = 'AccordionDetails';

// ============== Theme & Styling ==============

export const ThemeProvider = ({ children, theme }) => <>{children}</>;

export const useTheme = () => ({
  palette: {
    primary: { main: '#362759', light: '#5a4a7a', dark: '#1a1040', contrastText: '#fff' },
    secondary: { main: '#fcfafa', light: '#fff', dark: '#c9c7c7', contrastText: '#000' },
    background: { default: '#1B1B1B', paper: '#474350' },
    text: { primary: '#fcfafa', secondary: '#b0b0b0' },
    error: { main: '#f44336' },
    warning: { main: '#ff9800' },
    success: { main: '#4caf50' },
    info: { main: '#2196f3' },
    divider: 'rgba(255,255,255,0.12)',
    mode: 'dark',
  },
  spacing: (n) => `${n * 8}px`,
  breakpoints: {
    up: () => '@media (min-width:0px)',
    down: () => '@media (min-width:0px)',
    between: () => '@media (min-width:0px)',
    values: { xs: 0, sm: 600, md: 900, lg: 1200, xl: 1536 },
  },
  typography: { fontFamily: '"Roboto","Helvetica","Arial",sans-serif' },
  shape: { borderRadius: 4 },
  transitions: { create: () => 'none', duration: { shortest: 0, shorter: 0, short: 0, standard: 0 } },
});

export const useMediaQuery = () => false;

export const createTheme = (options) => ({ ...options });

export const styled = (Component) => {
  const factory = (..._args) => {
    if (typeof Component === 'string') {
      const StyledComp = React.forwardRef((props, ref) => React.createElement(Component, { ref, ...props }));
      StyledComp.displayName = `Styled(${Component})`;
      return StyledComp;
    }
    const StyledComp = React.forwardRef((props, ref) => <Component ref={ref} {...props} />);
    StyledComp.displayName = `Styled(${Component.displayName || Component.name || 'Component'})`;
    return StyledComp;
  };
  factory.withConfig = () => factory;
  factory.attrs = () => factory;
  return factory;
};

export const alpha = (color, opacity) => color;
export const darken = (color, amount) => color;
export const lighten = (color, amount) => color;

// Default export for sub-module imports like `import useMediaQuery from '@mui/material/useMediaQuery'`
export default useMediaQuery;
