﻿using ImGalaxy.ES.Core;
using OrderContext.Domain.Customers;
using OrderContext.Domain.Messages.Orders;
using OrderContext.Domain.Orders.Snapshots;
using OrderContext.Domain.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrderContext.Domain.Orders
{
    public class OrderState : AggregateRootState<OrderState>, ISnapshotable
    {
        public OrderId Id { get; private set; }

        private AddresState _address;

        private CustomerId _buyerId;

        private DateTime _orderDate;

        private List<OrderItemState> _orderItems;
        public OrderStatus OrderStatus { get; private set; }

        private OrderState()
        {
            RegisterEvent<OrderStartedEvent>(When);
            RegisterEvent<OrderPaidEvent>(When);
            RegisterEvent<OrderShippedEvent>(When);
            RegisterEvent<OrderCancelledEvent>(When);
            RegisterEvent<OrderItemAddedEvent>(When);
        } 

        internal OrderState(OrderId id, CustomerId buyerId) : this() =>
            EnsureValidState(id, buyerId);

        private void EnsureValidState(OrderId id, CustomerId buyerId) =>
           this.ThrowsIf(_=> string.IsNullOrEmpty(id), new ArgumentNullException(id))
               .ThrowsIf(_=> string.IsNullOrEmpty(buyerId), new ArgumentNullException(buyerId));


        private void When(OrderStartedEvent @event) =>
           With(this, state =>
           {
               state.Id = new OrderId(@event.OrderId);
               state._buyerId = new CustomerId(@event.BuyerId);
               state._address = Address.Create(@event.Street, @event.City, string.Empty, string.Empty, string.Empty);
               state.OrderStatus = OrderStatus.Submitted;
               state._orderDate = DateTime.Now;
           }); 

        private void When(OrderCancelledEvent @event) =>
           With(this, state =>
           {
               state.OrderStatus = OrderStatus.Cancelled;
           });

        private void When(OrderShippedEvent @event) =>
           With(this, state =>
           {
               state.OrderStatus = OrderStatus.Shipped;
           });

        private void When(OrderPaidEvent @event) =>
           With(this, state =>
           {
               state.OrderStatus = OrderStatus.Paid;
           });

        private void When(OrderItemAddedEvent @event) =>
           With(this, state =>
           {
               var newItem = OrderItem.Create(new OrderItemId(@event.OrderItemId), state.Id, new ProductId(@event.ProductId), @event.UnitPrice, @event.Discount);

               state._orderItems = state._orderItems ?? new List<OrderItemState>();

               state._orderItems.Add(newItem.State);

           });

        public void RestoreSnapshot(object stateSnapshot) =>
            With(this, state=> 
            {
                var snapshot = (OrderStateSnapshot)stateSnapshot;
                state.Id = new OrderId(snapshot.Id);
                state._buyerId = new CustomerId(snapshot.BuyerId);
                state._orderDate = snapshot.OrderDate;
                state._address = state._address ?? Address.Create(snapshot.Street, snapshot.City, string.Empty, string.Empty, string.Empty);
                state._orderItems = snapshot.OrderItems.Select(item=> OrderItem.Create(new OrderItemId(item.Id), state.Id, new ProductId(item.ProductId), item.UnitPrice, item.Discount).State)
                                    .ToList();
            });

        public object TakeSnapshot()=>
            new OrderStateSnapshot
            {
                Id = this.Id,
                BuyerId = this._buyerId,
                City = this._address.City,
                OrderDate = this._orderDate,
                Street = this._address.Street,
                OrderStatus = this.OrderStatus.Name,
                OrderItems = this._orderItems.Select(item=> item.TakeSnapshot() as OrderItemStateSnapshot)
            };
    }
}
