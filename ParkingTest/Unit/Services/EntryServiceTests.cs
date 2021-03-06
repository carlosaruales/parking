﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Moq;
using Application.Interfaces;
using AppCore.Entities;
using ParkingTest.Builders;
using Application.DTOs;
using AppCore.Exceptions;
using AppCore.Enums;
using System.Collections.Generic;
using Application.Mappers;
using System.Linq;

namespace Application.Services.Tests
{
    [TestClass()]
    public class EntryServiceTests
    {
        public Mock<IRepository<EntryEntity>> entryRepository;
        public Mock<IRepository<DepartureEntity>> departureRepository;

        public Mock<IEntryService> entryService;
        public Mock<ICellService> _cellService;
        public Mock<IDepartureService> _departureService;
        public Mock<IPlacaService> _placaService;

        [TestInitialize]
        public void Setup()
        {
            // repositories
            entryRepository = new Mock<IRepository<EntryEntity>>();
            departureRepository = new Mock<IRepository<DepartureEntity>>();


            entryService = new Mock<IEntryService>();
            _cellService = new Mock<ICellService>();
            _departureService = new Mock<IDepartureService>();
            _placaService = new Mock<IPlacaService>();
        }

        [TestMethod()]
        public void RegisterMotorcycle_WithoutCC_ShouldReturnException()
        {
            // Arrange (preparación, organizar)
            var entryBuilder = new EntryDTOBuilder();
            DtoEntry entry = entryBuilder
                            .WithCC(null)
                            .Build();

            // cuando se ejecute este método en el entry service va a retornar true en la validacion de ExistsQuotaByVehicleType
            _cellService.Setup(cs => cs.ExistsQuotaByVehicleType(VehicleTypeEnum.motorcycle)).Returns(true);
            _placaService.Setup(ps => ps.GetLastNumberOfIdVehicle(entry.IdVehicleType, entry.IdVehicle)).Returns("5");

            var entryService = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);
            

            // Act
            try
            {
                entryService.RegistryVehicle(entry);
            }
            catch (Exception e)
            {
                var message = "Falta la información del cilindraje de la motocicleta";
                // Assert (confirmacion)
                Assert.IsInstanceOfType(e, typeof(EntryException));
                Assert.AreEqual(e.Message, message);
            }
        }

        [TestMethod()]
        public void RegisterVehicle_WithoutCellAvaliable_ShouldReturnException()
        {
            // Arrange (preparación, organizar)
            var entryBuilder = new EntryDTOBuilder()
                .WithVehicleId("SFL555")
                .WithVehicleType(VehicleTypeEnum.car);

            _cellService.Setup(cs => cs.ExistsQuotaByVehicleType(VehicleTypeEnum.car)).Returns(false);


            var entryService = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);
            DtoEntry entry = entryBuilder.Build();

            // Act
            try
            {
                entryService.RegistryVehicle(entry);
            }
            catch (Exception e)
            {
                var message = "No hay cupos disponibles";
                // Assert (confirmacion)
                Assert.IsInstanceOfType(e, typeof(CellException));
                Assert.AreEqual(e.Message, message);
            }
        }

        [TestMethod()]
        public void RegisterVehicle_WithPendingDeparture_ShouldReturnException()
        {
            // Arrange (preparación, organizar)
            var entryBuilder = new EntryDTOBuilder()
                .WithVehicleId("SFL55D")
                .WithVehicleType(VehicleTypeEnum.motorcycle)
                .WithCC("1000");
            var uniqueId = Guid.NewGuid().ToString();
            var entryList = new List<EntryEntity>();
           
            var entryEntity = new EntryEntityBuilder().Build();
            entryList.Add(entryEntity);
            var departureEntity = new DepartureEntityBuilder()
                                  .WithIdEntry(uniqueId)
                                  .Build();
            DtoEntry entry = entryBuilder.Build();

            entryRepository.Setup(er => er.List(e => e.IdVehicle == entry.IdVehicle)).Returns(entryList);

            var entryService = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);
            

            // Act
            try
            {
                entryService.RegistryVehicle(entry);
            }
            catch (Exception e)
            {
                var message = "El vehículo que está registrando posee una salida pendiente";
                // Assert (confirmacion)
                Assert.IsInstanceOfType(e, typeof(EntryException));
                Assert.AreEqual(e.Message, message);
            }
        }

        [TestMethod()]
        public void RegisterMotorcycle_WithBadIdVehicleFormat_ShouldReturnException()
        {
            // Arrange (preparación, organizar)
            var entryBuilder = new EntryDTOBuilder()
                .WithVehicleId("SFL555")
                .WithVehicleType(VehicleTypeEnum.motorcycle)
                .WithCC("1000");

            DtoEntry entry = entryBuilder.Build();
            _cellService.Setup(cs => cs.ExistsQuotaByVehicleType(VehicleTypeEnum.motorcycle)).Returns(true);
            var entryService = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            try
            {
                entryService.RegistryVehicle(entry);
            }
            catch (Exception e)
            {
                var message = "Hubo un problema al leer la placa del vehículo. Verifique el tipo de vehículo e intente de nuevo";
                // Assert (confirmacion)
                Assert.IsInstanceOfType(e, typeof(EntryException));
                Assert.AreEqual(e.Message, message);
            }
        }

        [TestMethod()]
        public void RegisterCar_WithBadIdVehicleFormat_ShouldReturnException()
        {
            // Arrange (preparación, organizar)
            var entryBuilder = new EntryDTOBuilder()
                .WithVehicleId("SFL55A")
                .WithVehicleType(VehicleTypeEnum.car);

            DtoEntry entry = entryBuilder.Build();
            _cellService.Setup(cs => cs.ExistsQuotaByVehicleType(VehicleTypeEnum.car)).Returns(true);
            var entryService = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            try
            {
                entryService.RegistryVehicle(entry);
            }
            catch (Exception e)
            {
                var message = "Hubo un problema al leer la placa del vehículo. Verifique el tipo de vehículo e intente de nuevo";
                // Assert (confirmacion)
                Assert.IsInstanceOfType(e, typeof(EntryException));
                Assert.AreEqual(e.Message, message);
            }
        }

        [TestMethod()]
        public void GetLastEntryByVehicleId_ShouldReturnAnEntityList()
        {
            // Arrange (preparación, organizar)
            var entryEntity = new EntryEntity
            {
                CC = null,
                EntryTime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                IdVehicle = "SFL55D",
                IdVehicleType = VehicleTypeEnum.car
            };
            var entryList = new List<EntryEntity>
            {
                entryEntity
            };
            var id = entryEntity.IdVehicle;
            entryRepository.Setup(er => er.List(er => er.IdVehicle == id)).Returns(entryList);

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);
            // Act
            var result = entryServiceClass.GetLastEntryByVehicleId(entryEntity.IdVehicle);


            // Assert
            Assert.IsTrue(result.GetType() == typeof(EntryEntity));
        }

        [TestMethod()]
        public void GetLastEntryByVehicleBadId_ShouldReturnNull()
        {
            // Arrange (preparación, organizar)
            var entryEntity = new EntryEntity
            {
                CC = null,
                EntryTime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                IdVehicle = "SFL55D",
                IdVehicleType = VehicleTypeEnum.car
            };
            var entryList = new List<EntryEntity>
            {
                entryEntity
            };
            var id = entryEntity.IdVehicle;
            entryRepository.Setup(er => er.List(er => er.IdVehicle == id)).Returns(entryList);

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            var result = entryServiceClass.GetLastEntryByVehicleId("ZFL55D");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void GetEntryById_ShouldReturnADTOEntry()
        {
            // Arrange
            var entryEntity = new EntryEntity
            {
                CC = null,
                EntryTime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                IdVehicle = "SFL55D",
                IdVehicleType = VehicleTypeEnum.car
            };
            var id = entryEntity.Id;
            entryRepository.Setup(er => er.GetById(id)).Returns(entryEntity);

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            var result = entryServiceClass.GetEntryById(id);

            // Assert
            Assert.IsTrue(result.GetType() == typeof(DtoEntry));
        }

        [TestMethod()]
        public void GetEntryByBadId_ShouldReturnAnEntityWithValuesInNull()
        {
            // Arrange
            var entryEntity = new EntryEntity
            {
                CC = null,
                EntryTime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                IdVehicle = "SFL55D",
                IdVehicleType = VehicleTypeEnum.car
            };
            var id = "aa";
            entryRepository.Setup(er => er.GetById(entryEntity.Id)).Returns(entryEntity);

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            var result = entryServiceClass.GetEntryById(id);

            // Assert
            Assert.IsNull(result.Id);
        }

        [TestMethod()]
        public void GetEntries_ShouldNotReturnNullList()
        {
            // Arrange
            entryRepository.Setup(er => er.List()).Returns(new List<EntryEntity> { 
                new EntryEntity{ 
                    EntryTime = DateTime.Now,
                    Id = Guid.NewGuid().ToString(),
                    IdVehicle =  "SFL555",
                    IdVehicleType = VehicleTypeEnum.car
                }
            });

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            var result = entryServiceClass.GetEntries();

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void GetEntries_ShouldReturnListWith0Items()
        {
            // Arrange
            entryRepository.Setup(er => er.List()).Returns(new List<EntryEntity>());

            var entryServiceClass = new EntryService(entryRepository.Object, _cellService.Object, _departureService.Object, _placaService.Object);

            // Act
            IEnumerable<DtoEntry> result = entryServiceClass.GetEntries();

            // Assert
            Assert.IsTrue(result.Count() <= 0);
        }
    }
}