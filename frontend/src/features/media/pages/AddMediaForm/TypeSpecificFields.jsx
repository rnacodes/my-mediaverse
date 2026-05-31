import { useFormContext } from 'react-hook-form';
import BookFields from './BookFields';
import PodcastFields from './PodcastFields';
import MovieFields from './MovieFields';
import TVShowFields from './TVShowFields';
import VideoFields from './VideoFields';

// Renders the field block for the currently selected media type.
function TypeSpecificFields() {
  const { watch } = useFormContext();
  const mediaType = watch('mediaType');

  switch (mediaType) {
    case 'Book':
      return <BookFields />;
    case 'Podcast':
      return <PodcastFields />;
    case 'Movie':
      return <MovieFields />;
    case 'TVShow':
      return <TVShowFields />;
    case 'Video':
      return <VideoFields />;
    default:
      return null;
  }
}

export default TypeSpecificFields;
