use serde::{Deserialize, Serialize};
use std::fmt;
use std::str::FromStr;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum ResourceType {
    Application,
    File,
    Folder,
    Link,
}

impl ResourceType {
    pub fn as_str(self) -> &'static str {
        match self {
            Self::Application => "application",
            Self::File => "file",
            Self::Folder => "folder",
            Self::Link => "link",
        }
    }
}

impl fmt::Display for ResourceType {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(self.as_str())
    }
}

impl FromStr for ResourceType {
    type Err = String;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match s.trim().to_ascii_lowercase().as_str() {
            "application" | "app" => Ok(Self::Application),
            "file" => Ok(Self::File),
            "folder" | "dir" | "directory" => Ok(Self::Folder),
            "link" | "url" => Ok(Self::Link),
            other => Err(format!("Unsupported resource type: {other}")),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WorkspaceDto {
    pub id: String,
    pub name: String,
    pub icon: String,
    pub sort_order: i64,
    pub created_at: String,
    pub updated_at: String,
    pub resource_count: usize,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ResourceDto {
    pub id: String,
    pub workspace_id: String,
    pub name: String,
    #[serde(rename = "type")]
    pub resource_type: ResourceType,
    pub target: String,
    pub sort_order: i64,
    pub created_at: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AvailabilityResult {
    pub resource_id: String,
    pub available: bool,
    pub reason: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LaunchItemResult {
    pub resource_id: String,
    pub name: String,
    #[serde(rename = "type")]
    pub resource_type: ResourceType,
    pub target: String,
    pub success: bool,
    pub error_code: Option<String>,
    pub error_message: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct BatchLaunchSummary {
    pub workspace_id: String,
    pub total: usize,
    pub launched: usize,
    pub failed: usize,
    pub cancelled: bool,
    pub results: Vec<LaunchItemResult>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LaunchProgressEvent {
    pub workspace_id: String,
    pub completed: usize,
    pub total: usize,
    pub current_name: String,
    pub cancelled: bool,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct NewResourceInput {
    pub name: Option<String>,
    #[serde(rename = "type")]
    pub resource_type: Option<ResourceType>,
    pub target: String,
}
