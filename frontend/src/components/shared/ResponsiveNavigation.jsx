import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Box,
  useTheme,
  useMediaQuery,
  Chip,
  Menu,
  MenuItem,
  Collapse
} from '@mui/material';
import {
  Menu as MenuIcon,
  Close as CloseIcon,
  Home,
  Movie,
  QueueMusic,
  Add,
  Upload,
  Download,
  Search,
  Science,
  Apps,
  CleaningServices,
  Login as LoginIcon,
  Logout as LogoutIcon,
  Person as PersonIcon,
  Storage as StorageIcon,
  Article,
  Language,
  Category,
  AdminPanelSettings,
  Sync,
  ImportExport,
  ExpandMore,
  Work,
  ExpandLess,
  Book,
  Tv,
  Podcasts,
  VideoLibrary,
  YouTube,
  MusicNote,
  SportsEsports,
  Description,
  Note as NoteIcon,
  AutoAwesome,
  Psychology,
  Terminal,
  FormatQuote,
  AddLink,
  LockOpen,
  PlaylistAdd,
  Phonelink
} from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

const ResponsiveNavigation = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [browseMediaMenuAnchor, setBrowseMediaMenuAnchor] = useState(null);
  const [mediaMenuAnchor, setMediaMenuAnchor] = useState(null);
  const [adminMenuAnchor, setAdminMenuAnchor] = useState(null);
  const [mobileBrowseMediaOpen, setMobileBrowseMediaOpen] = useState(false);
  const [mobileMediaOpen, setMobileMediaOpen] = useState(false);
  const [mobileAdminOpen, setMobileAdminOpen] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, user, logout } = useAuth();

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleNavigation = (path, requiresAuth = false) => {
    if (requiresAuth && !isAuthenticated) {
      navigate('/login');
    } else {
      navigate(path);
    }
    setMobileOpen(false);
    handleCloseMenus();
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
    setMobileOpen(false);
  };

  const handleLogin = () => {
    navigate('/login');
    setMobileOpen(false);
  };

  const handleCloseMenus = () => {
    setBrowseMediaMenuAnchor(null);
    setMediaMenuAnchor(null);
    setAdminMenuAnchor(null);
  };

  const navigationItems = [
    { text: 'Home', path: '/', icon: <Home /> },
    { text: 'Search', path: '/search', icon: <Search /> },
    { text: 'Topics and Genres', path: '/search-by-topic-genre', icon: <Category /> },
    { text: 'Mixlists', path: '/search?searchMode=mixlists', icon: <QueueMusic /> }
  ];

  const browseMediaMenuItems = [
    { text: 'Articles', path: '/search?mediaType=Article', icon: <Article /> },
    { text: 'Books', path: '/search?mediaType=Book', icon: <Book /> },
    { text: 'Channels', path: '/search?mediaType=Channel', icon: <YouTube /> },
    { text: 'Highlights', path: '/search?mediaType=Highlight', icon: <FormatQuote /> },
    { text: 'Movies', path: '/search?mediaType=Movie', icon: <Movie /> },
    { text: 'Notes', path: '/notes', icon: <NoteIcon /> },
    { text: 'Playlists', path: '/search?mediaType=Playlist', icon: <VideoLibrary /> },
    { text: 'Podcasts', path: '/search?mediaType=Podcast', icon: <Podcasts /> },
    { text: 'TV Shows', path: '/search?mediaType=TVShow', icon: <Tv /> },
    { text: 'Videos', path: '/search?mediaType=Video', icon: <VideoLibrary /> },
    { text: 'Websites', path: '/search?mediaType=Website', icon: <Language /> }
  ];

  const mediaMenuItems = [
    { text: 'Media Form', path: '/add-media', icon: <Add /> },
    { text: 'API Import', path: '/import-media', icon: <Download /> },
    { text: 'Import Website', path: '/import-website', icon: <Language /> },
    { text: 'Bulk Upload', path: '/upload-media', icon: <Upload /> },
    { text: 'Create Mixlist', path: '/create-mixlist', icon: <PlaylistAdd /> },
    { text: 'Readwise Sync', path: '/readwise-sync', icon: <Sync />, requiresAuth: true },
    { text: 'Trakt Sync', path: '/trakt-sync', icon: <Phonelink />, requiresAuth: true }
  ];

  const adminMenuItems = [
    { text: 'AI Admin', path: '/ai-admin', icon: <Psychology />, requiresAuth: true },
    { text: 'Background Jobs', path: '/background-jobs', icon: <Work />, requiresAuth: true },
    { text: 'Cleanup', path: '/cleanup', icon: <CleaningServices />, requiresAuth: true },
    { text: 'Demo Admin', path: '/demo-unlock', icon: <LockOpen />, requiresAuth: true },
    { text: 'Local Scripts', path: '/script-execution', icon: <Terminal />, requiresAuth: true },
    { text: 'Readwise Sync', path: '/readwise-sync', icon: <Sync />, requiresAuth: true },
    { text: 'Link Highlights', path: '/highlight-linking', icon: <AddLink />, requiresAuth: true },
    { text: 'Sources', path: '/sources', icon: <Apps />, requiresAuth: true },
    { text: 'Typesense Admin', path: '/typesense-admin', icon: <StorageIcon />, requiresAuth: true }
  ];

  const drawer = (
    <Box sx={{ width: 280 }}>
      <Box sx={{ 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'space-between',
        p: 2,
        borderBottom: 1,
        borderColor: 'divider'
      }}>
        <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
          My MediaVerse
        </Typography>
        <IconButton onClick={handleDrawerToggle}>
          <CloseIcon />
        </IconButton>
      </Box>
      
      {/* User Info / Auth Section */}
      {isAuthenticated && user && (
        <Box sx={{ p: 2, backgroundColor: 'action.hover' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <PersonIcon fontSize="small" />
            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
              Logged In
            </Typography>
          </Box>
        </Box>
      )}
      
      <List>
        {/* Main Navigation Items */}
        {navigationItems.map((item) => (
          <ListItem
            button
            key={item.text}
            onClick={() => handleNavigation(item.path)}
            sx={{
              '&:hover': {
                backgroundColor: 'action.hover',
              }
            }}
          >
            <ListItemIcon sx={{ color: '#fcfafa' }}>
              {item.icon}
            </ListItemIcon>
            <ListItemText 
              primary={item.text}
              sx={{
                '& .MuiListItemText-primary': {
                  fontWeight: 'medium'
                }
              }}
            />
          </ListItem>
        ))}
        
        {/* Browse Media Submenu */}
        <ListItem
          button
          onClick={() => setMobileBrowseMediaOpen(!mobileBrowseMediaOpen)}
          sx={{
            '&:hover': {
              backgroundColor: 'action.hover',
            }
          }}
        >
          <ListItemIcon sx={{ color: '#fcfafa' }}>
            <Movie />
          </ListItemIcon>
          <ListItemText 
            primary="Browse Media"
            sx={{
              '& .MuiListItemText-primary': {
                fontWeight: 'medium'
              }
            }}
          />
          {mobileBrowseMediaOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItem>
        <Collapse in={mobileBrowseMediaOpen} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {browseMediaMenuItems.map((item) => (
              <ListItem
                button
                key={item.text}
                onClick={() => handleNavigation(item.path)}
                sx={{
                  pl: 4,
                  '&:hover': {
                    backgroundColor: 'action.hover',
                  }
                }}
              >
                <ListItemIcon sx={{ color: '#fcfafa', minWidth: 40 }}>
                  {item.icon}
                </ListItemIcon>
                <ListItemText 
                  primary={item.text}
                  sx={{
                    '& .MuiListItemText-primary': {
                      fontSize: '0.9rem'
                    }
                  }}
                />
              </ListItem>
            ))}
          </List>
        </Collapse>
        
        {/* Add Media Submenu */}
        <ListItem
          button
          onClick={() => setMobileMediaOpen(!mobileMediaOpen)}
          sx={{
            '&:hover': {
              backgroundColor: 'action.hover',
            }
          }}
        >
          <ListItemIcon sx={{ color: '#fcfafa' }}>
            <Add />
          </ListItemIcon>
          <ListItemText
            primary="Add Media"
            sx={{
              '& .MuiListItemText-primary': {
                fontWeight: 'medium'
              }
            }}
          />
          {mobileMediaOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItem>
        <Collapse in={mobileMediaOpen} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {mediaMenuItems.map((item) => (
              <ListItem
                button
                key={item.text}
                onClick={() => handleNavigation(item.path)}
                sx={{
                  pl: 4,
                  '&:hover': {
                    backgroundColor: 'action.hover',
                  }
                }}
              >
                <ListItemIcon sx={{ color: '#fcfafa', minWidth: 40 }}>
                  {item.icon}
                </ListItemIcon>
                <ListItemText 
                  primary={item.text}
                  sx={{
                    '& .MuiListItemText-primary': {
                      fontSize: '0.9rem'
                    }
                  }}
                />
              </ListItem>
            ))}
          </List>
        </Collapse>
        
        {/* Admin Submenu */}
        <ListItem
          button
          onClick={() => setMobileAdminOpen(!mobileAdminOpen)}
          sx={{
            '&:hover': {
              backgroundColor: 'action.hover',
            }
          }}
        >
          <ListItemIcon sx={{ color: '#fcfafa' }}>
            <AdminPanelSettings />
          </ListItemIcon>
          <ListItemText 
            primary="Admin"
            sx={{
              '& .MuiListItemText-primary': {
                fontWeight: 'medium'
              }
            }}
          />
          {mobileAdminOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItem>
        <Collapse in={mobileAdminOpen} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {adminMenuItems.map((item) => (
              <ListItem
                button
                key={item.text}
                onClick={() => handleNavigation(item.path, item.requiresAuth)}
                sx={{
                  pl: 4,
                  '&:hover': {
                    backgroundColor: 'action.hover',
                  }
                }}
              >
                <ListItemIcon sx={{ color: '#fcfafa', minWidth: 40 }}>
                  {item.icon}
                </ListItemIcon>
                <ListItemText 
                  primary={item.text}
                  sx={{
                    '& .MuiListItemText-primary': {
                      fontSize: '0.9rem'
                    }
                  }}
                />
              </ListItem>
            ))}
          </List>
        </Collapse>
        
        {/* Login/Logout Button in Mobile Menu */}
        {isAuthenticated ? (
          <ListItem
            button
            onClick={handleLogout}
            sx={{
              '&:hover': {
                backgroundColor: 'action.hover',
              },
              borderTop: 1,
              borderColor: 'divider',
              mt: 1
            }}
          >
            <ListItemIcon sx={{ color: 'error.main' }}>
              <LogoutIcon />
            </ListItemIcon>
            <ListItemText 
              primary="Logout"
              sx={{
                '& .MuiListItemText-primary': {
                  fontWeight: 'medium',
                  color: 'error.main'
                }
              }}
            />
          </ListItem>
        ) : (
          <ListItem
            button
            onClick={handleLogin}
            sx={{
              '&:hover': {
                backgroundColor: 'action.hover',
              },
              borderTop: 1,
              borderColor: 'divider',
              mt: 1
            }}
          >
            <ListItemIcon sx={{ color: '#fcfafa' }}>
              <LoginIcon />
            </ListItemIcon>
            <ListItemText 
              primary="Login"
              sx={{
                '& .MuiListItemText-primary': {
                  fontWeight: 'medium'
                }
              }}
            />
          </ListItem>
        )}
      </List>
    </Box>
  );

  return (
    <>
      <AppBar position="static" sx={{ backgroundColor: 'background.paper' }}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            <Button color="inherit" component={Link} to="/" sx={{ textTransform: 'none', fontSize: '1.25rem' }}>
              My MediaVerse
            </Button>
          </Typography>
          
          {isMobile ? (
            <IconButton
              color="inherit"
              aria-label="open drawer"
              edge="start"
              onClick={handleDrawerToggle}
            >
              <MenuIcon />
            </IconButton>
          ) : (
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              {/* Main Navigation Items */}
              {navigationItems.map((item) => (
                <Button
                  key={item.text}
                  color="inherit"
                  component={Link}
                  to={item.path}
                  sx={{ 
                    textTransform: 'none',
                    minWidth: 'auto',
                    px: 2
                  }}
                >
                  {item.text}
                </Button>
              ))}
              
              {/* Browse Media Menu */}
              <Button
                color="inherit"
                onClick={(e) => setBrowseMediaMenuAnchor(e.currentTarget)}
                endIcon={<ExpandMore />}
                sx={{ 
                  textTransform: 'none',
                  minWidth: 'auto',
                  px: 2
                }}
              >
                Browse Media
              </Button>
              <Menu
                anchorEl={browseMediaMenuAnchor}
                open={Boolean(browseMediaMenuAnchor)}
                onClose={handleCloseMenus}
              >
                {browseMediaMenuItems.map((item) => (
                  <MenuItem
                    key={item.text}
                    onClick={() => handleNavigation(item.path)}
                  >
                    <ListItemIcon>
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText>{item.text}</ListItemText>
                  </MenuItem>
                ))}
              </Menu>
              
              {/* Add Media Menu */}
              <Button
                color="inherit"
                onClick={(e) => setMediaMenuAnchor(e.currentTarget)}
                endIcon={<ExpandMore />}
                sx={{
                  textTransform: 'none',
                  minWidth: 'auto',
                  px: 2
                }}
              >
                Add Media
              </Button>
              <Menu
                anchorEl={mediaMenuAnchor}
                open={Boolean(mediaMenuAnchor)}
                onClose={handleCloseMenus}
              >
                {mediaMenuItems.map((item) => (
                  <MenuItem
                    key={item.text}
                    onClick={() => handleNavigation(item.path)}
                  >
                    <ListItemIcon>
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText>{item.text}</ListItemText>
                  </MenuItem>
                ))}
              </Menu>
              
              {/* Admin Menu */}
              <Button
                color="inherit"
                onClick={(e) => setAdminMenuAnchor(e.currentTarget)}
                endIcon={<ExpandMore />}
                sx={{ 
                  textTransform: 'none',
                  minWidth: 'auto',
                  px: 2
                }}
              >
                Admin
              </Button>
              <Menu
                anchorEl={adminMenuAnchor}
                open={Boolean(adminMenuAnchor)}
                onClose={handleCloseMenus}
              >
                {adminMenuItems.map((item) => (
                  <MenuItem
                    key={item.text}
                    onClick={() => handleNavigation(item.path, item.requiresAuth)}
                  >
                    <ListItemIcon>
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText>{item.text}</ListItemText>
                  </MenuItem>
                ))}
              </Menu>
              
              {/* Desktop Auth Section */}
              {isAuthenticated && user ? (
                <>
                  <Chip
                    icon={<PersonIcon />}
                    label="Logged In"
                    size="small"
                    sx={{ ml: 2 }}
                  />
                  <Button
                    color="inherit"
                    onClick={handleLogout}
                    startIcon={<LogoutIcon />}
                    sx={{ 
                      textTransform: 'none',
                      color: 'error.main',
                      '&:hover': {
                        backgroundColor: 'error.light',
                        color: 'error.contrastText'
                      }
                    }}
                  >
                    Logout
                  </Button>
                </>
              ) : (
                <Button
                  color="inherit"
                  onClick={handleLogin}
                  startIcon={<LoginIcon />}
                  sx={{ 
                    textTransform: 'none',
                    ml: 2
                  }}
                >
                  Login
                </Button>
              )}
            </Box>
          )}
        </Toolbar>
      </AppBar>

      <Drawer
        variant="temporary"
        anchor="right"
        open={mobileOpen}
        onClose={handleDrawerToggle}
        ModalProps={{
          keepMounted: true, // Better open performance on mobile.
        }}
        sx={{
          display: { xs: 'block', md: 'none' },
          '& .MuiDrawer-paper': {
            boxSizing: 'border-box',
            width: 280,
          },
        }}
      >
        {drawer}
      </Drawer>
    </>
  );
};

export default ResponsiveNavigation;
