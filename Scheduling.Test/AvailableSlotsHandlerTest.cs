using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Infrastructure.InMemory;
using Scheduling.Domain.Application;
using Scheduling.Domain.Domain.DoctorDay.Events;
using Scheduling.Domain.Domain.ReadModel;
using Scheduling.Test.Test;
using Xunit;
using EventHandler = Scheduling.Domain.Infrastructure.Projections.EventHandler;

namespace Scheduling.Test
{
    public class AvailableSlotsHandlerTest : HandlerTest
    {
        private static InMemoryAvailableSlotsRepository _repository;

        private readonly DateTime _now = DateTime.UtcNow;

        private TimeSpan _tenMinutes = TimeSpan.FromMinutes(10);

        protected override EventHandler GetHandler()
        {
            _repository = new InMemoryAvailableSlotsRepository();
            _repository.Clear();
            return new AvailableSlotsProjection(_repository);
        }

        [Fact]
        public async Task should_add_slot_to_the_list()
        {
            var scheduled = new SlotScheduled(Guid.NewGuid(), "dayId", _now, _tenMinutes);
            await Given(scheduled);
            Then(new List<AvailableSlot>
            {
                new AvailableSlot
                {
                    Date = scheduled.SlotStartTime.Date,
                    Duration = scheduled.SlotDuration,
                    Id = scheduled.SlotId.ToString(),
                    DayId = scheduled.DayId,
                    IsBooked = false,
                    StartTime = scheduled.SlotStartTime
                }
            }, await _repository.GetAvailableSlotsOn(_now));
        }

        [Fact]
        public async Task should_hide_the_slot_from_list_if_booked()
        {
            var scheduled = new SlotScheduled(Guid.NewGuid(), "dayId", _now, _tenMinutes);
            await Given(
                scheduled,
                new SlotBooked("dayId", scheduled.SlotId, "PatientId"));
            Then(new List<AvailableSlot>(), await _repository.GetAvailableSlotsOn(_now));
        }

        [Fact]
        public async Task should_show_slot_if_booking_was_cancelled()
        {
            var scheduled = new SlotScheduled(Guid.NewGuid(), "dayId", _now, _tenMinutes);
            await Given(
                scheduled,
                new SlotBooked("dayId", scheduled.SlotId, "PatientId"),
                new SlotBookingCancelled("dayId", scheduled.SlotId, "Reason"));
            Then(new List<AvailableSlot>
            {
                new AvailableSlot
                {
                    Date = scheduled.SlotStartTime.Date,
                    Duration = scheduled.SlotDuration,
                    Id = scheduled.SlotId.ToString(),
                    DayId = scheduled.DayId,
                    IsBooked = false,
                    StartTime = scheduled.SlotStartTime
                }
            }, await _repository.GetAvailableSlotsOn(_now));
        }

        [Fact]
        public async Task should_delete_slot_if_slot_was_cancelled()
        {
            var scheduled = new SlotScheduled(Guid.NewGuid(), "dayId", _now, _tenMinutes);
            await Given(
                scheduled,
                new SlotScheduleCancelled("dayId", scheduled.SlotId));
            Then(new List<AvailableSlot>(), await _repository.GetAvailableSlotsOn(_now));
        }
    }
}
