"""
Unit tests for worklist.py
"""

import pytest
from scheduler import (
    Worklist, PlateTask, TransferOverview, Transfer, TransferTasks,
    Plate, create_worklist_from_transfer_overview
)


class TestPlateTask:
    """Tests for PlateTask"""
    
    def test_plate_task_creation(self):
        """Test creating a PlateTask"""
        task = PlateTask("Bumblebee", "source_hitpick")
        assert task.device_type == "Bumblebee"
        assert task.command == "source_hitpick"
        assert task.completed == False
        assert task.parameters == {}
    
    def test_plate_task_with_parameters(self):
        """Test creating a PlateTask with parameters"""
        params = {"volume": 10.0, "speed": "fast"}
        task = PlateTask("Processor", "process", parameters=params)
        assert task.parameters == params
        assert task.parameters["volume"] == 10.0
    
    def test_plate_task_repr(self):
        """Test PlateTask string representation"""
        task = PlateTask("Bumblebee", "source_hitpick")
        assert "Bumblebee" in repr(task)
        assert "source_hitpick" in repr(task)
    
    def test_plate_task_completed(self):
        """Test marking task as completed"""
        task = PlateTask("Bumblebee", "source_hitpick")
        assert task.completed == False
        task.completed = True
        assert task.completed == True


class TestTransferTasks:
    """Tests for TransferTasks"""
    
    def test_transfer_tasks_creation(self):
        """Test creating TransferTasks"""
        tasks = TransferTasks()
        assert tasks.source_prehitpick_tasks == []
        assert tasks.source_posthitpick_tasks == []
        assert tasks.destination_prehitpick_tasks == []
        assert tasks.destination_posthitpick_tasks == []
    
    def test_transfer_tasks_with_tasks(self):
        """Test TransferTasks with actual tasks"""
        pre_task = PlateTask("Processor", "preprocess")
        post_task = PlateTask("Processor", "postprocess")
        
        tasks = TransferTasks(
            source_prehitpick_tasks=[pre_task],
            source_posthitpick_tasks=[post_task]
        )
        assert len(tasks.source_prehitpick_tasks) == 1
        assert len(tasks.source_posthitpick_tasks) == 1
        assert tasks.source_prehitpick_tasks[0] == pre_task


class TestTransfer:
    """Tests for Transfer"""
    
    def test_transfer_creation(self):
        """Test creating a Transfer"""
        source = Plate("SRC001", "96-well", "96-well")
        dest = Plate("DST001", "96-well", "96-well")
        transfer = Transfer(source, dest)
        
        assert transfer.source_plate == source
        assert transfer.destination_plate == dest
        assert transfer.source_well is None
        assert transfer.destination_well is None
        assert transfer.volume is None
    
    def test_transfer_with_details(self):
        """Test creating a Transfer with all details"""
        source = Plate("SRC001", "96-well", "96-well")
        dest = Plate("DST001", "96-well", "96-well")
        transfer = Transfer(
            source, dest,
            source_well="A1",
            destination_well="B2",
            volume=10.0
        )
        
        assert transfer.source_well == "A1"
        assert transfer.destination_well == "B2"
        assert transfer.volume == 10.0


class TestTransferOverview:
    """Tests for TransferOverview"""
    
    def test_transfer_overview_creation(self):
        """Test creating a TransferOverview"""
        overview = TransferOverview()
        assert overview.transfers == []
        assert overview.source_plates == {}
        assert overview.destination_plates == {}
        assert isinstance(overview.tasks, TransferTasks)
    
    def test_transfer_overview_with_data(self):
        """Test TransferOverview with transfers and plates"""
        source = Plate("SRC001", "96-well", "96-well")
        dest = Plate("DST001", "96-well", "96-well")
        transfer = Transfer(source, dest)
        
        overview = TransferOverview(
            transfers=[transfer],
            source_plates={"SRC001": source},
            destination_plates={"DST001": dest}
        )
        
        assert len(overview.transfers) == 1
        assert overview.transfers[0] == transfer
        assert overview.source_plates["SRC001"] == source
        assert overview.destination_plates["DST001"] == dest


class TestWorklist:
    """Tests for Worklist"""
    
    def test_worklist_creation(self):
        """Test creating a Worklist"""
        worklist = Worklist("TestWorklist")
        assert worklist.name == "TestWorklist"
        assert worklist.source_plates == []
        assert worklist.destination_plates == []
        assert worklist.transfer_overview is None
    
    def test_add_source_plate(self):
        """Test adding source plates"""
        worklist = Worklist("TestWorklist")
        plate = Plate("SRC001", "96-well", "96-well")
        
        worklist.add_source_plate(plate)
        assert len(worklist.source_plates) == 1
        assert worklist.source_plates[0] == plate
    
    def test_add_destination_plate(self):
        """Test adding destination plates"""
        worklist = Worklist("TestWorklist")
        plate = Plate("DST001", "96-well", "96-well")
        
        worklist.add_destination_plate(plate)
        assert len(worklist.destination_plates) == 1
        assert worklist.destination_plates[0] == plate
    
    def test_add_multiple_plates(self):
        """Test adding multiple plates"""
        worklist = Worklist("TestWorklist")
        
        for i in range(3):
            source = Plate(f"SRC{i:03d}", "96-well", "96-well")
            dest = Plate(f"DST{i:03d}", "96-well", "96-well")
            worklist.add_source_plate(source)
            worklist.add_destination_plate(dest)
        
        assert len(worklist.source_plates) == 3
        assert len(worklist.destination_plates) == 3
    
    def test_worklist_completion_callback(self):
        """Test worklist completion callbacks"""
        worklist = Worklist("TestWorklist")
        callback_called = []
        
        def callback(wl):
            callback_called.append(wl)
        
        worklist.add_completion_callback(callback)
        worklist.on_worklist_complete()
        
        assert len(callback_called) == 1
        assert callback_called[0] == worklist
    
    def test_worklist_multiple_callbacks(self):
        """Test multiple completion callbacks"""
        worklist = Worklist("TestWorklist")
        callbacks_called = []
        
        for i in range(3):
            def make_callback(idx):
                def callback(wl):
                    callbacks_called.append(idx)
                return callback
            worklist.add_completion_callback(make_callback(i))
        
        worklist.on_worklist_complete()
        assert len(callbacks_called) == 3
        assert set(callbacks_called) == {0, 1, 2}
    
    def test_worklist_repr(self):
        """Test Worklist string representation"""
        worklist = Worklist("TestWorklist")
        plate = Plate("SRC001", "96-well", "96-well")
        worklist.add_source_plate(plate)
        
        repr_str = repr(worklist)
        assert "TestWorklist" in repr_str
        assert "1 sources" in repr_str


class TestCreateWorklistFromTransferOverview:
    """Tests for create_worklist_from_transfer_overview"""
    
    def test_create_worklist_from_overview(self):
        """Test creating worklist from transfer overview"""
        source = Plate("SRC001", "96-well", "96-well")
        dest = Plate("DST001", "96-well", "96-well")
        transfer = Transfer(source, dest)
        
        overview = TransferOverview(
            transfers=[transfer],
            source_plates={"SRC001": source},
            destination_plates={"DST001": dest}
        )
        
        worklist = create_worklist_from_transfer_overview("TestWorklist", overview)
        
        assert worklist.name == "TestWorklist"
        assert worklist.transfer_overview == overview
        assert len(worklist.source_plates) == 1
        assert len(worklist.destination_plates) == 1
        assert worklist.source_plates[0] == source
        assert worklist.destination_plates[0] == dest
    
    def test_create_worklist_multiple_plates(self):
        """Test creating worklist with multiple plates"""
        sources = [Plate(f"SRC{i:03d}", "96-well", "96-well") for i in range(3)]
        dests = [Plate(f"DST{i:03d}", "96-well", "96-well") for i in range(2)]
        
        transfers = [
            Transfer(sources[i % 3], dests[i % 2])
            for i in range(5)
        ]
        
        overview = TransferOverview(
            transfers=transfers,
            source_plates={p.barcode: p for p in sources},
            destination_plates={p.barcode: p for p in dests}
        )
        
        worklist = create_worklist_from_transfer_overview("TestWorklist", overview)
        
        assert len(worklist.source_plates) == 3
        assert len(worklist.destination_plates) == 2

