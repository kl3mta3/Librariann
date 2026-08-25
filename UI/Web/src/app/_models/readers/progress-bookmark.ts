export interface ProgressBookmark {
    pageNum: number;
    chapterId: number;
    bookScrollId?: string;
    /**
     * For Audiobook reader, playback position in seconds to resume from.
     */
    playbackPositionSeconds?: number | null;
}