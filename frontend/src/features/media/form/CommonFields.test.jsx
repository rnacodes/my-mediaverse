import { describe, it, expect } from 'vitest';
import { useForm, FormProvider } from 'react-hook-form';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import CommonFields from './CommonFields';
import { defaultValues } from './schema';

// Articles and Websites are created from their source (Readwise sync, the website importer),
// so the type selector must not offer them as things to type in.
function Harness({ values = {} }) {
  const methods = useForm({ defaultValues: { ...defaultValues, ...values } });
  return (
    <FormProvider {...methods}>
      <CommonFields />
    </FormProvider>
  );
}

describe('CommonFields media type selector', () => {
  it('offers only the types the form can create', async () => {
    const { user } = renderWithProviders(<Harness />, { route: '/add-media' });

    await user.click(screen.getByTestId('media-type-select').querySelector('[role="combobox"]') ?? screen.getByRole('combobox'));

    const listbox = await screen.findByRole('listbox');
    const options = within(listbox).getAllByRole('option').map((o) => o.textContent);
    expect(options).toEqual(['Book', 'Movie', 'Podcast', 'TV Show', 'Video']);
    expect(options.join(' ')).not.toMatch(/coming soon/i);
  });
});

describe('CommonFields ownership status', () => {
  it('hides the Ownership Status select for a Video and shows it for a Book', () => {
    const { unmount } = renderWithProviders(<Harness values={{ mediaType: 'Video' }} />, { route: '/add-media' });
    expect(screen.queryByLabelText(/ownership status/i)).not.toBeInTheDocument();
    unmount();

    renderWithProviders(<Harness values={{ mediaType: 'Book' }} />, { route: '/add-media' });
    expect(screen.getByLabelText(/ownership status/i)).toBeInTheDocument();
  });
});
