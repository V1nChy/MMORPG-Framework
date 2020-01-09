using CodeX;
using System;
using System.ComponentModel;
using System.Net;

public class ProgressEventProxy
{
    public DownloadInfo downloadInfo;
    public Action<DownloadInfo, DownloadProgressChangedEventArgs> progressMethod;
    public Action<DownloadInfo, AsyncCompletedEventArgs> completeMethod;

    public void OnProgrssChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		this.progressMethod(this.downloadInfo, e);
	}

	public void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
	{
		this.completeMethod(this.downloadInfo, e);
	}
}
