﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sessions.Models;
using SessionsVoting.API.Models;
using SessionsVoting.API.Services;

namespace SessionsVoting.API.Controllers
{
    [Route("api/votings")]
    public class VotingsController : Controller
    {
        private IVotingsService Service { get; }
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();

        public VotingsController(IVotingsService service)
        {
            Service = service;
        }

        [Route("{sessionId}")]
        [HttpGet]
        public IActionResult GetVotingSummary(Guid sessionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (sessionId == Guid.Empty)
            {
                return BadRequest("SessionId is an empty Guid");
            }

            try
            {
                var votings = Service.GetVotingsBySessionId(sessionId).ToList();
                var votingSummary = new VotingSummary
                {
                    SessionId = sessionId,
                    UpVotes = votings.Where(v => v.Value > 0)
                        .Aggregate(0, (acc, x) => acc + x.Value),
                    DownVotes = votings.Where(v => v.Value < 0)
                        .Aggregate(0, (acc, x) => acc + (x.Value * -1))
                };
                return Ok(votingSummary);
            }
            catch (IndexOutOfRangeException)
            {
                return BadRequest();
            }
        }

        [Route("{sessionId}")]
        [HttpPost]
        public IActionResult Vote(Guid sessionId, [FromBody] VotingModel votingModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (sessionId == Guid.Empty || votingModel == null)
            {
                return BadRequest();
            }

            try
            {
                var voting = new Voting
                {
                    Date = DateTime.Now,
                    SessionId = sessionId,
                    Id = Guid.NewGuid(),
                    Value = votingModel.Change
                };
                Service.AddVoting(sessionId, voting);
                _telemetryClient.TrackEvent("VotedForSession", new Dictionary<string, string>
                {
                    {"sessionId", sessionId.ToString()},
                    {"change", votingModel.Change.ToString()}
                });
                return Ok(new {Success = true});
            }
            catch (Exception exception)
            {
                _telemetryClient.TrackException(exception);
                return StatusCode(500);
            }
            finally
            {
                _telemetryClient.Flush();
            }
        }
    }
}
