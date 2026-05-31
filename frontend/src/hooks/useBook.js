import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllBooks,
  getBookById,
  getBooksByAuthor,
  getBookSeries,
  createBook,
  updateBook,
  deleteBook,
  searchBooksFromOpenLibrary,
  importBookFromOpenLibrary,
  searchBooksFromGoogleBooks,
  importBookFromGoogleBooks,
} from '../api/bookService';
import { bookKeys, mediaKeys } from '../api/queryKeys';

export function useAllBooks(options = {}) {
  return useQuery({
    queryKey: bookKeys.lists(),
    queryFn: async () => (await getAllBooks()).data,
    ...options,
  });
}

export function useBook(id, options = {}) {
  return useQuery({
    queryKey: bookKeys.detail(id),
    queryFn: async () => (await getBookById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useBooksByAuthor(author, options = {}) {
  return useQuery({
    queryKey: [...bookKeys.lists(), { author }],
    queryFn: async () => (await getBooksByAuthor(author)).data,
    enabled: !!author,
    ...options,
  });
}

export function useBookSeries(options = {}) {
  return useQuery({
    queryKey: [...bookKeys.all, 'series'],
    queryFn: async () => (await getBookSeries()).data,
    ...options,
  });
}

export function useOpenLibrarySearch(searchParams, options = {}) {
  return useQuery({
    queryKey: [...bookKeys.all, 'openLibrarySearch', searchParams],
    queryFn: () => searchBooksFromOpenLibrary(searchParams),
    enabled: !!searchParams?.query,
    ...options,
  });
}

export function useGoogleBooksSearch(searchParams, options = {}) {
  return useQuery({
    queryKey: [...bookKeys.all, 'googleBooksSearch', searchParams],
    queryFn: () => searchBooksFromGoogleBooks(searchParams),
    enabled: !!searchParams?.query,
    ...options,
  });
}

export function useCreateBook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (bookData) => createBook(bookData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateBook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, bookData }) => updateBook(id, bookData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: bookKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteBook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteBook(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.removeQueries({ queryKey: bookKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportBookFromOpenLibrary() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (importData) => importBookFromOpenLibrary(importData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportBookFromGoogleBooks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (importData) => importBookFromGoogleBooks(importData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
