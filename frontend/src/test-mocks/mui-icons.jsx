import React from 'react';

const createIcon = (name) => {
  const Icon = React.forwardRef((props, ref) => (
    <span ref={ref} data-testid={`${name}Icon`} aria-hidden="true" {...props} />
  ));
  Icon.displayName = `${name}Icon`;
  Icon.muiName = 'SvgIcon';
  return Icon;
};

// Navigation
export const Menu = createIcon('Menu');
export const Close = createIcon('Close');
export const Home = createIcon('Home');
export const ArrowBack = createIcon('ArrowBack');
export const ArrowForward = createIcon('ArrowForward');
export const ArrowForwardIos = createIcon('ArrowForwardIos');
export const ExpandMore = createIcon('ExpandMore');
export const ExpandLess = createIcon('ExpandLess');
export const ChevronLeft = createIcon('ChevronLeft');
export const ChevronRight = createIcon('ChevronRight');

// Media
export const Movie = createIcon('Movie');
export const Tv = createIcon('Tv');
export const Book = createIcon('Book');
export const Article = createIcon('Article');
export const LibraryMusic = createIcon('LibraryMusic');
export const Podcasts = createIcon('Podcasts');
export const SportsEsports = createIcon('SportsEsports');
export const YouTube = createIcon('YouTube');
export const Language = createIcon('Language');
export const MenuBook = createIcon('MenuBook');
export const AutoAwesome = createIcon('AutoAwesome');
export const VideoLibrary = createIcon('VideoLibrary');
export const MusicNote = createIcon('MusicNote');
export const PlayArrow = createIcon('PlayArrow');
export const Pause = createIcon('Pause');
export const Stop = createIcon('Stop');
export const AutoStories = createIcon('AutoStories');
export const MovieFilter = createIcon('MovieFilter');
export const QueueMusic = createIcon('QueueMusic');

// Actions
export const Add = createIcon('Add');
export const AddCircleOutline = createIcon('AddCircleOutline');
export const AddLink = createIcon('AddLink');
export const Search = createIcon('Search');
export const Edit = createIcon('Edit');
export const Delete = createIcon('Delete');
export const Save = createIcon('Save');
export const Cancel = createIcon('Cancel');
export const Clear = createIcon('Clear');
export const OpenInNew = createIcon('OpenInNew');
export const Share = createIcon('Share');
export const Download = createIcon('Download');
export const FileDownload = createIcon('FileDownload');
export const FileUpload = createIcon('FileUpload');
export const Upload = createIcon('Upload');
export const CloudUpload = createIcon('CloudUpload');
export const Sync = createIcon('Sync');
export const Refresh = createIcon('Refresh');
export const ImportExport = createIcon('ImportExport');
export const ContentCopy = createIcon('ContentCopy');
export const Remove = createIcon('Remove');
export const PlaylistAdd = createIcon('PlaylistAdd');
export const BookmarkAdd = createIcon('BookmarkAdd');

// Info & Status
export const Info = createIcon('Info');
export const Help = createIcon('Help');
export const Settings = createIcon('Settings');
export const AccountCircle = createIcon('AccountCircle');
export const CheckCircle = createIcon('CheckCircle');
export const Error = createIcon('Error');
export const Warning = createIcon('Warning');
export const Block = createIcon('Block');
export const Visibility = createIcon('Visibility');
export const AccessTime = createIcon('AccessTime');
export const Schedule = createIcon('Schedule');

// UI Controls
export const FilterList = createIcon('FilterList');
export const ViewModule = createIcon('ViewModule');
export const ViewList = createIcon('ViewList');
export const Sort = createIcon('Sort');
export const TuneRounded = createIcon('TuneRounded');
export const MoreVert = createIcon('MoreVert');
export const Inbox = createIcon('Inbox');
export const Archive = createIcon('Archive');

// Business
export const Storage = createIcon('Storage');
export const Science = createIcon('Science');
export const AdminPanelSettings = createIcon('AdminPanelSettings');
export const Work = createIcon('Work');
export const Category = createIcon('Category');
export const Topic = createIcon('Topic');
export const Code = createIcon('Code');
export const Description = createIcon('Description');
export const Note = createIcon('Note');
export const NoteAlt = createIcon('NoteAlt');
export const Notes = createIcon('Notes');
export const RssFeed = createIcon('RssFeed');

// People & Auth
export const Person = createIcon('Person');
export const Login = createIcon('Login');
export const Logout = createIcon('Logout');
export const Lock = createIcon('Lock');
export const LockOpen = createIcon('LockOpen');
export const Key = createIcon('Key');
export const Psychology = createIcon('Psychology');
export const Terminal = createIcon('Terminal');
export const LocalLibrary = createIcon('LocalLibrary');
export const CleaningServices = createIcon('CleaningServices');
export const Apps = createIcon('Apps');

// Device & Input
export const PhoneAndroid = createIcon('PhoneAndroid');
export const QrCode = createIcon('QrCode');
export const Numbers = createIcon('Numbers');
export const Timer = createIcon('Timer');

// Rating & Favorites
export const Favorite = createIcon('Favorite');
export const Star = createIcon('Star');
export const ThumbUp = createIcon('ThumbUp');
export const ThumbDown = createIcon('ThumbDown');
export const CheckBox = createIcon('CheckBox');
export const CheckBoxOutlineBlank = createIcon('CheckBoxOutlineBlank');

// Files & Folders
export const Folder = createIcon('Folder');
export const FileCopy = createIcon('FileCopy');

// Misc
export const AutoFixHigh = createIcon('AutoFixHigh');
export const Dns = createIcon('Dns');
export const FormatQuote = createIcon('FormatQuote');

// Default export for sub-module default imports like `import X from '@mui/icons-material/X'`
export default createIcon('Default');
