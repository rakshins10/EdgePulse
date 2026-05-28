import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { AlertCountDto } from '../types/alerts';

interface AlertsState {
  count: AlertCountDto;
  lastFetchedAt: number | null;
}

const initialState: AlertsState = {
  count: { openCount: 0, criticalOpenCount: 0 },
  lastFetchedAt: null,
};

const alertsSlice = createSlice({
  name: 'alerts',
  initialState,
  reducers: {
    setAlertCount(state, action: PayloadAction<AlertCountDto>) {
      state.count = action.payload;
      state.lastFetchedAt = Date.now();
    },
  },
});

export const { setAlertCount } = alertsSlice.actions;
export default alertsSlice.reducer;
