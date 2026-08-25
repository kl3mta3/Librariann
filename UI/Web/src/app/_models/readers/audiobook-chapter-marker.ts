/**
 * One embedded chapter marker extracted from an M4B's chapter atoms via ffprobe at scan time. Used to render
 * scrubber tick marks and drive in-file prev/next-chapter seeking for single-file audiobooks.
 */
export interface AudiobookChapterMarker {
  title?: string | null;
  startSeconds: number;
  endSeconds: number;
}
