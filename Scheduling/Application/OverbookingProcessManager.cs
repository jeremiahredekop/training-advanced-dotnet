using System;
using Scheduling.Domain.DoctorDay.Commands;
using Scheduling.Domain.DoctorDay.Events;
using Scheduling.Domain.Domain.ReadModel;
using Scheduling.EventSourcing;
using EventHandler = Scheduling.Infrastructure.Projections.EventHandler;


namespace Scheduling.Domain.Application
{
    public class OverbookingProcessManager : EventHandler
    {
        public const int MaxVisitThreshold = 3;

        public OverbookingProcessManager(IBookedSlotsRepository bookedSlotRepository, int bookingLimitedPerPatient,
            ICommandStore commandStore, Func<Guid> idGenerator)
        {
            When<SlotScheduled>(async (e, m) =>
            {
                await bookedSlotRepository.AddSlot(new BookedSlot()
                {
                    Id = e.SlotId.ToString(),
                    DayId = e.DayId,
                    IsBooked = false,
                    Month = e.SlotStartTime.Month
                });
            });

            When<SlotBooked>(async (e, m) =>
            {
                await bookedSlotRepository.MarkSlotAsBooked(e.SlotId.ToString(), e.PatientId);                
                var slot = await bookedSlotRepository.GetSlot(e.SlotId.ToString());
                var bookings = await bookedSlotRepository.CountByPatientAndMonth(e.PatientId, slot.Month);

                if (bookings > MaxVisitThreshold)
                {
                    await commandStore.Send(new CancelSlotBooking(e.DayId, e.SlotId, "overbooked"),
                        new CommandMetadata(m.CorrelationId, m.CausationId));
                }
            });

            When<SlotBookingCancelled>(async (e, m) =>
            {
                await bookedSlotRepository.MarkSlotAsAvailable(e.SlotId.ToString());
            });
        }
    }
}
