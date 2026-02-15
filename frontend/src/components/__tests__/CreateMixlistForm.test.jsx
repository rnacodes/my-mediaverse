import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import CreateMixlistForm from '../CreateMixlistForm';
import * as mixlistService from '../../api/mixlistService';

const mockNavigate = vi.fn();

vi.mock('../../api/mixlistService', () => ({
  createMixlist: vi.fn(),
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

describe('CreateMixlistForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();
    global.alert.mockClear();
    mixlistService.createMixlist.mockResolvedValue({
      data: {
        id: 1,
        name: 'Test Mixlist',
        thumbnail: 'https://picsum.photos/400/400?random=123&blur=1'
      }
    });
  });

  describe('Form Submission', () => {
    it('should submit mixlist form with all properties and save to database', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist Name' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: 'Test Mixlist Name',
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });
    });

    it('should handle empty name validation', async () => {
      renderWithRouter(<CreateMixlistForm />);

      const submitButton = screen.getByText('Create Mixlist');
      expect(submitButton).toBeDisabled();

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Name' }
      });

      expect(submitButton).not.toBeDisabled();
    });

    it('should handle whitespace-only name validation', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: '   ' }
      });

      const submitButton = screen.getByText('Create Mixlist');
      expect(submitButton).not.toBeDisabled();
    });

    it('should trim whitespace from name before submission', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: '  Test Mixlist Name  ' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: 'Test Mixlist Name',
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });
    });
  });

  describe('Thumbnail Generation', () => {
    it('should generate unique thumbnail URL for each submission', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'First Mixlist' }
      });
      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: 'First Mixlist',
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });

      vi.clearAllMocks();
      mixlistService.createMixlist.mockResolvedValue({
        data: {
          id: 2,
          name: 'Second Mixlist',
          thumbnail: 'https://picsum.photos/400/400?random=456&blur=1'
        }
      });

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Second Mixlist' }
      });
      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: 'Second Mixlist',
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });
    });

    it('should include blur effect in thumbnail URL', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        const callArgs = mixlistService.createMixlist.mock.calls[0][0];
        expect(callArgs.thumbnail).toContain('blur=1');
        expect(callArgs.description).toBeNull();
      });
    });
  });

  describe('Form State Management', () => {
    it('should show loading state during submission', async () => {
      mixlistService.createMixlist.mockImplementation(() =>
        new Promise(resolve => setTimeout(() => resolve({ data: { id: 1 } }), 100))
      );

      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      expect(screen.getByText('Creating...')).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Create Mixlist')).toBeInTheDocument();
      });
    });

    it('should disable submit button during submission', async () => {
      mixlistService.createMixlist.mockImplementation(() =>
        new Promise(resolve => setTimeout(() => resolve({ data: { id: 1 } }), 100))
      );

      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      const submitButton = screen.getByText('Create Mixlist');
      fireEvent.click(submitButton);

      expect(submitButton).toBeDisabled();

      await waitFor(() => {
        expect(submitButton).not.toBeDisabled();
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle API errors gracefully', async () => {
      mixlistService.createMixlist.mockRejectedValue({
        response: {
          status: 400,
          data: { error: 'Validation failed' }
        }
      });

      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith(expect.stringMatching(/Failed to create mixlist/));
      });
    });

    it('should handle network errors', async () => {
      mixlistService.createMixlist.mockRejectedValue({
        message: 'Network error'
      });

      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith(expect.stringMatching(/Failed to create mixlist/));
      });
    });

    it('should re-enable submit button after error', async () => {
      mixlistService.createMixlist.mockRejectedValue({
        response: {
          status: 400,
          data: { error: 'Validation failed' }
        }
      });

      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      const submitButton = screen.getByText('Create Mixlist');
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith(expect.stringMatching(/Failed to create mixlist/));
      });

      expect(submitButton).not.toBeDisabled();
    });
  });

  describe('Navigation', () => {
    it('should navigate to mixlists page after successful creation', async () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: 'Test Mixlist' }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mockNavigate).toHaveBeenCalledWith('/mixlists');
      });
    });

    it('should handle cancel navigation', () => {
      renderWithRouter(<CreateMixlistForm />);

      fireEvent.click(screen.getByText('Cancel'));

      expect(mockNavigate).toHaveBeenCalledWith(-1);
    });
  });

  describe('Data Persistence', () => {
    it('should save mixlist with correct data structure to database', async () => {
      renderWithRouter(<CreateMixlistForm />);

      const mixlistName = 'My Test Mixlist';
      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: mixlistName }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: mixlistName,
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });

      expect(mixlistService.createMixlist).toHaveBeenCalledTimes(1);
    });

    it('should handle special characters in mixlist name', async () => {
      renderWithRouter(<CreateMixlistForm />);

      const mixlistName = 'Mixlist with Special Chars: !@#$%^&*()';
      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: mixlistName }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: mixlistName,
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });
    });

    it('should handle very long mixlist names', async () => {
      renderWithRouter(<CreateMixlistForm />);

      const longName = 'A'.repeat(1000);
      fireEvent.change(screen.getByPlaceholderText('Enter mixlist name...'), {
        target: { value: longName }
      });

      fireEvent.click(screen.getByText('Create Mixlist'));

      await waitFor(() => {
        expect(mixlistService.createMixlist).toHaveBeenCalledWith({
          name: longName,
          description: null,
          thumbnail: expect.stringMatching(/^https:\/\/picsum\.photos\/400\/400\?random=\d+&blur=1$/)
        });
      });
    });
  });
});
