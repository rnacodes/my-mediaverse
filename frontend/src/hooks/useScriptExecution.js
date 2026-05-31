import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  checkScriptRunnerHealth,
  getScriptJobs,
  getScriptJob,
  runNormalizeNotes,
  runNormalizeVault,
  cancelScriptJob,
} from '../api/scriptExecutionService';
import { scriptExecutionKeys } from '../api/queryKeys';

// ----- Queries -----

export function useScriptRunnerHealth(options = {}) {
  return useQuery({
    queryKey: scriptExecutionKeys.health(),
    queryFn: () => checkScriptRunnerHealth(),
    ...options,
  });
}

export function useScriptJobs(limit = 50, options = {}) {
  return useQuery({
    queryKey: scriptExecutionKeys.jobs(limit),
    queryFn: () => getScriptJobs(limit),
    ...options,
  });
}

export function useScriptJob(jobId, options = {}) {
  return useQuery({
    queryKey: scriptExecutionKeys.job(jobId),
    queryFn: () => getScriptJob(jobId),
    enabled: !!jobId,
    ...options,
  });
}

// ----- Mutations -----

export function useRunNormalizeNotes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (scriptOptions) => runNormalizeNotes(scriptOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scriptExecutionKeys.all });
    },
  });
}

export function useRunNormalizeVault() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (scriptOptions) => runNormalizeVault(scriptOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scriptExecutionKeys.all });
    },
  });
}

export function useCancelScriptJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId) => cancelScriptJob(jobId),
    onSuccess: (_data, jobId) => {
      queryClient.invalidateQueries({ queryKey: scriptExecutionKeys.job(jobId) });
      queryClient.invalidateQueries({ queryKey: scriptExecutionKeys.all });
    },
  });
}
