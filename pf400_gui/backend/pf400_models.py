"""
PF400 Model Definitions and Factory

Supports multiple PF400 robot models:
- PF400SX: Standard 5-joint arm (J1-J5)
- PF400SXL: Extended arm with 2m rail (J1-J6, where J6 is the rail)
"""

from enum import Enum
from typing import Dict, Any, Optional
from abc import ABC, abstractmethod


class PF400Model(Enum):
    """PF400 robot model variants"""
    SX = "400SX"      # Standard 5-joint arm
    SXL = "400SXL"    # Extended arm with 2m rail (6 joints)


class PF400ModelConfig:
    """Configuration for a specific PF400 model"""
    
    def __init__(
        self,
        model: PF400Model,
        num_joints: int,
        has_rail: bool = False,
        rail_length_mm: float = 0.0,
        rail_joint_index: int = 6,
        description: str = ""
    ):
        self.model = model
        self.num_joints = num_joints
        self.has_rail = has_rail
        self.rail_length_mm = rail_length_mm
        self.rail_joint_index = rail_joint_index
        self.description = description
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert config to dictionary"""
        return {
            "model": self.model.value,
            "num_joints": self.num_joints,
            "has_rail": self.has_rail,
            "rail_length_mm": self.rail_length_mm,
            "rail_joint_index": self.rail_joint_index,
            "description": self.description
        }


# Model configurations
MODEL_CONFIGS = {
    PF400Model.SX: PF400ModelConfig(
        model=PF400Model.SX,
        num_joints=5,
        has_rail=False,
        description="PF400 Standard - 5 joint arm (J1: vertical, J2: shoulder, J3: elbow, J4: wrist, J5: gripper)"
    ),
    PF400Model.SXL: PF400ModelConfig(
        model=PF400Model.SXL,
        num_joints=6,
        has_rail=True,
        rail_length_mm=2000.0,  # 2 meter rail
        rail_joint_index=6,
        description="PF400 Extended - 6 joint arm with 2m rail (J1: vertical, J2: shoulder, J3: elbow, J4: wrist, J5: gripper, J6: rail)"
    )
}


def get_model_config(model: PF400Model) -> PF400ModelConfig:
    """Get configuration for a model"""
    return MODEL_CONFIGS.get(model)


def get_model_by_name(name: str) -> Optional[PF400Model]:
    """Get model enum by name string"""
    name_upper = name.upper()
    if "SXL" in name_upper or "400SXL" in name_upper:
        return PF400Model.SXL
    elif "SX" in name_upper or "400SX" in name_upper:
        return PF400Model.SX
    return None


class DiagnosticsInterface(ABC):
    """Interface for robot diagnostics"""
    
    @abstractmethod
    def get_diagnostics(self) -> Dict[str, Any]:
        """Get comprehensive diagnostics information"""
        pass
    
    @abstractmethod
    def get_system_state(self) -> Dict[str, Any]:
        """Get current system state"""
        pass
    
    @abstractmethod
    def get_joint_states(self) -> Dict[str, Any]:
        """Get detailed joint state information"""
        pass
    
    @abstractmethod
    def get_error_log(self) -> list:
        """Get error log"""
        pass
    
    @abstractmethod
    def clear_errors(self) -> bool:
        """Clear error log"""
        pass

