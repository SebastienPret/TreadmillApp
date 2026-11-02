package mono.android.media;


public class MediaPlayer_OnCompletionListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.media.MediaPlayer.OnCompletionListener
{

	public MediaPlayer_OnCompletionListenerImplementor ()
	{
		super ();
		if (getClass () == MediaPlayer_OnCompletionListenerImplementor.class) {
			mono.android.TypeManager.Activate ("Android.Media.MediaPlayer+IOnCompletionListenerImplementor, Mono.Android", "", this, new java.lang.Object[] {  });
		}
	}

	public void onCompletion (android.media.MediaPlayer p0)
	{
		n_onCompletion (p0);
	}

	private native void n_onCompletion (android.media.MediaPlayer p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
