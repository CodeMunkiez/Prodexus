# MES Data Structure Specification
*Optimized for Raw-to-Structured Data Transformation Pipelines*

This document outlines the standard relational target schema for an automated Manufacturing Execution System (MES) ingestion pipeline. Undefined incoming payloads should be normalized, validated, and mapped directly into these four core tables to support genealogy, traceability, and OEE metrics.

---

## 1. Production Context Table (`production_context`)
Tracks the operational envelope of the manufacturing floor when an event occurs. This maps "Who, What, and Where".

| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `context_id` | UUID / BIGINT | PRIMARY KEY | Unique identifier for the transaction state. |
| `work_order_id` | VARCHAR(50) | NOT NULL | The active ERP/MES production order number. |
| `product_sku` | VARCHAR(50) | NOT NULL | Product or Material master stock keeping unit code. |
| `lot_number` | VARCHAR(50) | INDEXED | Batch or lot identifier for material traceability. |
| `serial_number` | VARCHAR(100) | UNIQUE, NULLABLE| Individual serial number for discrete manufacturing. |
| `operator_id` | VARCHAR(50) | NULLABLE | ID of the logged-in operator running the line. |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Record insertion timestamp in the database. |

---

## 2. Equipment Hierarchy Table (`equipment_hierarchy`)
Standardized physical asset lookup mapping to the ISA-95 model hierarchy.

| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `equipment_id` | VARCHAR(50) | PRIMARY KEY | Unique asset tag or machine ID. |
| `site_id` | VARCHAR(50) | NOT NULL | Geographic manufacturing facility identifier. |
| `area_id` | VARCHAR(50) | NOT NULL | Production area or department. |
| `production_line` | VARCHAR(50) | NOT NULL | The specific manufacturing line or cell. |
| `work_center` | VARCHAR(50) | NULLABLE | Specific sub-assembly station or process zone. |

---

## 3. Process Metrics Table (`process_metrics`)
Captures time-series telemetry and process data emitted during production.

| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `metric_id` | BIGINT | PRIMARY KEY | Auto-incrementing identifier. |
| `context_id` | UUID / BIGINT | FOREIGN KEY | References `production_context.context_id`. |
| `equipment_id` | VARCHAR(50) | FOREIGN KEY | References `equipment_hierarchy.equipment_id`. |
| `timestamp` | TIMESTAMP | INDEXED, NOT NULL | Actual time the event occurred on the shop floor. |
| `metric_name` | VARCHAR(100) | NOT NULL | Name of tag (e.g., 'hydraulic_pressure', 'cycle_time'). |
| `metric_value` | NUMERIC(12,4) | NOT NULL | Parsed, scaled, and normalized numerical value. |
| `uom` | VARCHAR(20) | NOT NULL | Standardized Unit of Measure (e.g., 'bar', 'sec', 'C'). |

---

## 4. Quality & Scrap Log Table (`quality_log`)
Logs defects, counts, and inspections for quality assurance analytics.

| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `log_id` | BIGINT | PRIMARY KEY | Unique log identifier. |
| `context_id` | UUID / BIGINT | FOREIGN KEY | References `production_context.context_id`. |
| `equipment_id` | VARCHAR(50) | FOREIGN KEY | References `equipment_hierarchy.equipment_id`. |
| `timestamp` | TIMESTAMP | NOT NULL | Time the quality event was triggered. |
| `total_count` | INT | NOT NULL | Quantity processed in the cycle or batch. |
| `scrap_count` | INT | DEFAULT 0 | Quantity rejected out of the total count. |
| `defect_code` | VARCHAR(30) | NULLABLE | Standardized reason code for the scrap event. |
| `is_reworkable` | BOOLEAN | DEFAULT FALSE | Flag indicating if product can go to a rework line. |

---

## ðŸ› ï¸ Data Pipeline Ingestion Checklist
1. **Idempotency Check:** Use a deterministic hashing strategy on raw payloads (`MD5(raw_payload + event_timestamp)`) to avoid duplicate processing in case of broker retries.
2. **Schema Registry:** Utilize a tool like Confluent Schema Registry or a JSON Schema validation gateway at the ingestion edge.
3. **Dead Letter Queue (DLQ):** Route unparseable raw formats to a DLQ partition with the parsing exception attached for rapid developer triage.

