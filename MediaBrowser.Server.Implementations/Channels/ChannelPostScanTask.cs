﻿using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelPostScanTask : ILibraryPostScanTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public ChannelPostScanTask(IChannelManager channelManager, IUserManager userManager, ILogger logger)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var users = _userManager.Users
                .DistinctBy(ChannelDownloadScheduledTask.GetUserDistinctValue)
                .Select(i => i.Id.ToString("N"))
                .ToList();

            var numComplete = 0;

            foreach (var user in users)
            {
                double percentPerUser = 1;
                percentPerUser /= users.Count;
                var startingPercent = numComplete * percentPerUser * 100;

                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(p => progress.Report(startingPercent + (percentPerUser * p)));

                await DownloadContent(user, cancellationToken, innerProgress).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= users.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task DownloadContent(string user, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var channels = await _channelManager.GetChannelsInternal(new ChannelQuery
            {
                UserId = user

            }, cancellationToken);

            var numComplete = 0;
            var numItems = channels.Items.Length;

            foreach (var channel in channels.Items)
            {
                var channelId = channel.Id.ToString("N");

                var features = _channelManager.GetChannelFeatures(channelId);

                const int currentRefreshLevel = 1;
                var maxRefreshLevel = features.AutoRefreshLevels ?? 1;

                var innerProgress = new ActionableProgress<double>();

                var startingNumberComplete = numComplete;
                innerProgress.RegisterAction(p =>
                {
                    double innerPercent = startingNumberComplete;
                    innerPercent += (p / 100);
                    innerPercent /= numItems;
                    progress.Report(innerPercent * 100);
                });

                try
                {
                    await GetAllItems(user, channelId, null, currentRefreshLevel, maxRefreshLevel, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channel content", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numItems;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task GetAllItems(string user, string channelId, string folderId, int currentRefreshLevel, int maxRefreshLevel, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var folderItems = new List<string>();

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(p / 2));

            var result = await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
            {
                ChannelId = channelId,
                UserId = user,
                FolderId = folderId

            }, innerProgress, cancellationToken);

            folderItems.AddRange(result.Items.Where(i => i.IsFolder).Select(i => i.Id.ToString("N")));

            var totalRetrieved = result.Items.Length;
            var totalCount = result.TotalRecordCount;

            while (totalRetrieved < totalCount)
            {
                result = await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
                {
                    ChannelId = channelId,
                    UserId = user,
                    StartIndex = totalRetrieved,
                    FolderId = folderId

                }, new Progress<double>(), cancellationToken);

                folderItems.AddRange(result.Items.Where(i => i.IsFolder).Select(i => i.Id.ToString("N")));

                totalRetrieved += result.Items.Length;
                totalCount = result.TotalRecordCount;
            }

            progress.Report(50);

            if (currentRefreshLevel < maxRefreshLevel)
            {
                var numComplete = 0;
                var numItems = folderItems.Count;

                foreach (var folder in folderItems)
                {
                    try
                    {
                        innerProgress = new ActionableProgress<double>();

                        var startingNumberComplete = numComplete;
                        innerProgress.RegisterAction(p =>
                        {
                            double innerPercent = startingNumberComplete;
                            innerPercent += (p / 100);
                            innerPercent /= numItems;
                            progress.Report((innerPercent * 50) + 50);
                        });

                        await GetAllItems(user, channelId, folder, currentRefreshLevel + 1, maxRefreshLevel, innerProgress, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting channel content", ex);
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= numItems;
                    progress.Report((percent * 50) + 50);
                }
            }

            progress.Report(100);
        }
    }
}
