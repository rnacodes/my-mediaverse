/**
 * True when a podcast item is a series rather than an episode. podcastType is
 * authoritative when the item carries it; older result shapes don't, and there
 * the absence of a parent seriesId is what marks a series.
 */
export const isPodcastSeries = (item) =>
    item.podcastType ? item.podcastType === 'Series' : !item.seriesId;
