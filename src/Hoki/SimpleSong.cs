using System;
using System.IO;
using Microsoft.Xna.Framework.Media;
using XnaSong=Microsoft.Xna.Framework.Media.Song;

namespace Hoki {
	/// <summary>
	/// Song implementation that plays a single looping stream through MediaPlayer.
	/// Missing files are tolerated: the original music is not preserved in this
	/// repository, so a SimpleSong without its file is simply silent.
	/// </summary>
	public class SimpleSong : Song {
		private XnaSong sample;

		public SimpleSong(string filename) {
			if (File.Exists(filename))
				sample=XnaSong.FromUri(Path.GetFileNameWithoutExtension(filename),new Uri(Path.GetFullPath(filename)));
			//ponytail: missing ogg => silent song, no crash; drop the file in music/ to restore audio
		}

		#region Song Members

		public override void Play() {
			if (sample==null) return;
			MediaPlayer.IsRepeating=true;
			MediaPlayer.Volume=Volume/100f;
			MediaPlayer.Play(sample);
		}

		public override void Stop() {
			if (sample==null) return;
			MediaPlayer.Stop();
		}

		#endregion
	}
}
