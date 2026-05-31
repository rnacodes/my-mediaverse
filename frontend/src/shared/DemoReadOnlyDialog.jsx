import React from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    Button,
    Box,
    Typography,
} from '@mui/material';
import { Block as BlockIcon } from '@mui/icons-material';
import { useDemoReadOnly } from '@/contexts/DemoReadOnlyContext';

const DemoReadOnlyDialog = () => {
    const { isDialogOpen, hideReadOnlyDialog } = useDemoReadOnly();

    return (
        <Dialog
            open={isDialogOpen}
            onClose={hideReadOnlyDialog}
            aria-labelledby="demo-readonly-dialog-title"
            aria-describedby="demo-readonly-dialog-description"
            maxWidth="sm"
            fullWidth
        >
            <DialogTitle id="demo-readonly-dialog-title">
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <BlockIcon sx={{ color: 'warning.main' }} />
                    <Typography variant="h6" component="span">
                        Demo Mode - Read Only
                    </Typography>
                </Box>
            </DialogTitle>
            <DialogContent>
                <DialogContentText id="demo-readonly-dialog-description">
                    The demo website is read-only. You can browse all content, but creating,
                    editing, or deleting data is disabled.
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button
                    onClick={hideReadOnlyDialog}
                    variant="contained"
                    color="primary"
                    // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses action button on dialog open
                    autoFocus
                >
                    Got it
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default DemoReadOnlyDialog;
