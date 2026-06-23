// Vault chip color, shared by the linked-notes display and the link dialog.
export function getVaultColor(vaultName) {
  switch (vaultName?.toLowerCase()) {
    case 'general':
      return '#4caf50';
    case 'programming':
      return '#2196f3';
    default:
      return '#9e9e9e';
  }
}
