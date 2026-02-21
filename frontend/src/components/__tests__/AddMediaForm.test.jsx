import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import AddMediaForm from '../AddMediaForm';
import * as mediaService from '../../api/mediaService';
import * as mixlistService from '../../api/mixlistService';
import * as podcastService from '../../api/podcastService';
import * as topicGenreService from '../../api/topicGenreService';
import * as bookService from '../../api/bookService';
import * as movieService from '../../api/movieService';
import * as tvShowService from '../../api/tvShowService';
import * as videoService from '../../api/videoService';
import * as uploadService from '../../api/uploadService';

const mockNavigate = vi.fn();

vi.mock('../../api/mediaService', () => ({
  addMedia: vi.fn(),
}));
vi.mock('../../api/mixlistService', () => ({
  getAllMixlists: vi.fn(),
  addMediaToMixlist: vi.fn(),
}));
vi.mock('../../api/podcastService', () => ({
  createPodcastEpisode: vi.fn(),
  searchPodcastSeries: vi.fn(),
}));
vi.mock('../../api/topicGenreService', () => ({
  searchTopics: vi.fn(),
  searchGenres: vi.fn(),
}));
vi.mock('../../api/bookService', () => ({
  createBook: vi.fn(),
}));
vi.mock('../../api/movieService', () => ({
  createMovie: vi.fn(),
}));
vi.mock('../../api/tvShowService', () => ({
  createTvShow: vi.fn(),
}));
vi.mock('../../api/videoService', () => ({
  createVideo: vi.fn(),
}));
vi.mock('../../api/uploadService', () => ({
  uploadThumbnail: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('AddMediaForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();

    mixlistService.getAllMixlists.mockResolvedValue({ data: [] });
    topicGenreService.searchTopics.mockResolvedValue({ data: [] });
    topicGenreService.searchGenres.mockResolvedValue({ data: [] });
    podcastService.searchPodcastSeries.mockResolvedValue({ data: [] });
  });

  it('should render the form with all core elements', () => {
    renderWithRouter(<AddMediaForm />);

    expect(screen.getByText('Add New Media')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter media title...')).toBeInTheDocument();
    expect(screen.getByText('Save Media')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByLabelText('Uncharted')).toBeInTheDocument();
    expect(screen.getByLabelText('Completed')).toBeInTheDocument();
  });

  it('should render optional form fields', () => {
    renderWithRouter(<AddMediaForm />);

    expect(screen.getByPlaceholderText('https://example.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Brief description of the media...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('https://example.com/thumbnail.jpg')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Add any notes or thoughts about this media...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Type to search mixlists...')).toBeInTheDocument();
  });

  describe('Form Validation', () => {
    const submitForm = () => {
      const form = screen.getByText('Save Media').closest('form');
      fireEvent.submit(form);
    };

    it('should show validation error when title is empty', async () => {
      renderWithRouter(<AddMediaForm />);

      submitForm();

      await waitFor(() => {
        expect(screen.getByTestId('title-error')).toBeInTheDocument();
      });
    });

    it('should show validation error when media type is not selected', async () => {
      renderWithRouter(<AddMediaForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter media title...'), {
        target: { value: 'Test Title' }
      });

      submitForm();

      await waitFor(() => {
        expect(screen.getByTestId('media-type-error')).toBeInTheDocument();
      });
    });

    it('should show both validation errors when title and media type are missing', async () => {
      renderWithRouter(<AddMediaForm />);

      submitForm();

      await waitFor(() => {
        expect(screen.getByTestId('title-error')).toBeInTheDocument();
        expect(screen.getByTestId('media-type-error')).toBeInTheDocument();
      });
    });
  });

  describe('Status Selection', () => {
    it('should default to Uncharted status', () => {
      renderWithRouter(<AddMediaForm />);

      const unchartedRadio = screen.getByLabelText('Uncharted');
      expect(unchartedRadio).toBeChecked();
    });

    it('should show Date Completed field when Completed status is selected', () => {
      renderWithRouter(<AddMediaForm />);

      expect(screen.queryByLabelText('Date Completed')).not.toBeInTheDocument();

      fireEvent.click(screen.getByLabelText('Completed'));

      expect(screen.getByLabelText('Date Completed')).toBeInTheDocument();
    });

    it('should hide Date Completed when switching away from Completed', () => {
      renderWithRouter(<AddMediaForm />);

      fireEvent.click(screen.getByLabelText('Completed'));
      expect(screen.getByLabelText('Date Completed')).toBeInTheDocument();

      fireEvent.click(screen.getByLabelText('Uncharted'));
      expect(screen.queryByLabelText('Date Completed')).not.toBeInTheDocument();
    });
  });

  describe('Form Inputs', () => {
    it('should accept title input', () => {
      renderWithRouter(<AddMediaForm />);

      const titleInput = screen.getByPlaceholderText('Enter media title...');
      fireEvent.change(titleInput, { target: { value: 'My Test Media' } });

      expect(titleInput.value).toBe('My Test Media');
    });

    it('should accept link input', () => {
      renderWithRouter(<AddMediaForm />);

      const linkInput = screen.getByPlaceholderText('https://example.com');
      fireEvent.change(linkInput, { target: { value: 'https://example.com/test' } });

      expect(linkInput.value).toBe('https://example.com/test');
    });

    it('should accept description input', () => {
      renderWithRouter(<AddMediaForm />);

      const descInput = screen.getByPlaceholderText('Brief description of the media...');
      fireEvent.change(descInput, { target: { value: 'A test description' } });

      expect(descInput.value).toBe('A test description');
    });

    it('should accept notes input', () => {
      renderWithRouter(<AddMediaForm />);

      const notesInput = screen.getByPlaceholderText('Add any notes or thoughts about this media...');
      fireEvent.change(notesInput, { target: { value: 'Some notes' } });

      expect(notesInput.value).toBe('Some notes');
    });
  });

  describe('Mixlist Section', () => {
    it('should render the Add to Mixlists section', () => {
      renderWithRouter(<AddMediaForm />);

      expect(screen.getByText('Add to Mixlists')).toBeInTheDocument();
      expect(screen.getByText('+ New Mixlist')).toBeInTheDocument();
    });

    it('should navigate to create mixlist page when clicking + New Mixlist', () => {
      renderWithRouter(<AddMediaForm />);

      fireEvent.click(screen.getByText('+ New Mixlist'));

      expect(mockNavigate).toHaveBeenCalledWith('/create-mixlist', { state: { returnTo: '/add-media' } });
    });
  });
});
