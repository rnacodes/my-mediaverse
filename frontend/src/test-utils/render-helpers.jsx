import React from 'react';
import { render } from '@testing-library/react';
import { BrowserRouter, MemoryRouter, Routes, Route } from 'react-router-dom';

/**
 * Render with BrowserRouter wrapper.
 */
export const renderWithRouter = (ui, options) => {
  return render(
    <BrowserRouter>{ui}</BrowserRouter>,
    options
  );
};

/**
 * Render with MemoryRouter (useful for testing with initial URL/params).
 */
export const renderWithMemoryRouter = (ui, { initialEntries = ['/'], ...options } = {}) => {
  return render(
    <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>,
    options
  );
};

/**
 * Render inside a route (useful for components using useParams).
 */
export const renderWithRoute = (ui, { path = '/', initialEntry = '/', ...options } = {}) => {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path={path} element={ui} />
      </Routes>
    </MemoryRouter>,
    options
  );
};

// Re-export everything from testing-library for convenience
export { screen, waitFor, fireEvent, act, within, cleanup } from '@testing-library/react';
export { default as userEvent } from '@testing-library/user-event';
