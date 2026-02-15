import React from 'react';

const styled = (Component) => {
  const factory = (..._args) => {
    if (typeof Component === 'string') {
      const StyledComp = React.forwardRef((props, ref) => React.createElement(Component, { ref, ...props }));
      StyledComp.displayName = `styled(${Component})`;
      return StyledComp;
    }
    const StyledComp = React.forwardRef((props, ref) => <Component ref={ref} {...props} />);
    StyledComp.displayName = `styled(${Component.displayName || Component.name || 'Component'})`;
    return StyledComp;
  };
  factory.withConfig = () => factory;
  factory.attrs = () => factory;
  return factory;
};

export default styled;
