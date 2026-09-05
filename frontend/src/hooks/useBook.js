import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getAllBooks, getBookById, createBook, updateBook } from '../api/bookService';
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
