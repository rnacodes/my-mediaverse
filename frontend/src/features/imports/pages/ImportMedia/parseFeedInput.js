/**
 * Work out what the "paste" box holds: a feed URL, an Apple Podcasts link
 * (…/id1234567), or a bare Apple Podcasts id. Returns the from-feed payload,
 * or null when the text is none of those.
 */
export const parseFeedInput = (raw) => {
    const value = (raw || '').trim();
    if (!value) return null;

    if (/^\d+$/.test(value)) return { applePodcastsId: value };

    const appleLink = value.match(/^https?:\/\/(?:[\w-]+\.)?apple\.com\/.*?\/id(\d+)/i);
    if (appleLink) return { applePodcastsId: appleLink[1] };

    if (/^https?:\/\/\S+$/i.test(value)) return { feedUrl: value };

    return null;
};
